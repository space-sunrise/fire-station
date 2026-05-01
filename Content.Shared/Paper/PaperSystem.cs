using System.Linq;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared._Scp.Knowledge; // Fire added - paper read events for SCP knowledge sources
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using static Content.Shared.Paper.PaperComponent;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Paper;

public sealed class PaperSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly ProtoId<TagPrototype> WriteIgnoreStampsTag = "WriteIgnoreStamps";
    private static readonly ProtoId<TagPrototype> WriteTag = "Write";
    private static readonly Vector2 DefaultImageScale = new (1f, 1f);

    private EntityQuery<PaperComponent> _paperQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PaperComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PaperComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PaperComponent, PaperInputTextMessage>(OnInputTextMessage);

        SubscribeLocalEvent<RandomPaperContentComponent, MapInitEvent>(OnRandomPaperContentMapInit);

        SubscribeLocalEvent<ActivateOnPaperOpenedComponent, PaperWriteEvent>(OnPaperWrite);

        _paperQuery = GetEntityQuery<PaperComponent>();
    }

    private void OnMapInit(Entity<PaperComponent> entity, ref MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(entity.Comp.Content))
        {
            SetContent(entity, Loc.GetString(entity.Comp.Content));
        }
    }

    private void OnInit(Entity<PaperComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);

        if (TryComp<AppearanceComponent>(entity, out var appearance))
        {
            if (entity.Comp.Content != "")
                _appearance.SetData(entity, PaperVisuals.Status, PaperStatus.Written, appearance);

            if (entity.Comp.StampState != null)
                _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
        }
    }

    private void BeforeUIOpen(Entity<PaperComponent> entity, ref BeforeActivatableUIOpenEvent args)
    {
        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
        // Fire added start - notify SCP knowledge sources about paper reading
        RaiseLocalEvent(new ScpKnowledgePaperReadEvent(entity.Owner, args.User, entity.Comp.Content));
        // Fire added end
    }

    private void OnExamined(Entity<PaperComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PaperComponent)))
        {
            if (entity.Comp.Content != "")
            {
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-has-words",
                        ("paper", entity)
                    )
                );
            }

            if (entity.Comp.StampedBy.Count > 0)
            {
                var commaSeparated =
                    string.Join(", ", entity.Comp.StampedBy.Select(s => Loc.GetString(s.StampedName)));
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-stamped-by",
                        ("paper", entity),
                        ("stamps", commaSeparated))
                );
            }
        }
    }

    private void OnInteractUsing(Entity<PaperComponent> entity, ref InteractUsingEvent args)
    {
        // only allow editing if there are no stamps or when using a cyberpen
        var editable = entity.Comp.StampedBy.Count == 0 || _tagSystem.HasTag(args.Used, WriteIgnoreStampsTag);
        if (_tagSystem.HasTag(args.Used, WriteTag))
        {
            if (editable)
            {
                if (entity.Comp.EditingDisabled)
                {
                    var paperEditingDisabledMessage = Loc.GetString("paper-tamper-proof-modified-message");
                    _popupSystem.PopupClient(paperEditingDisabledMessage, entity, args.User);

                    args.Handled = true;
                    return;
                }

                var ev = new PaperWriteAttemptEvent(entity.Owner);
                RaiseLocalEvent(args.User, ref ev);
                if (ev.Cancelled)
                {
                    if (ev.FailReason is not null)
                    {
                        var fileWriteMessage = Loc.GetString(ev.FailReason);
                        _popupSystem.PopupClient(fileWriteMessage, entity.Owner, args.User);
                    }

                    args.Handled = true;
                    return;
                }

                var writeEvent = new PaperWriteEvent(args.User, entity);
                RaiseLocalEvent(args.Used, ref writeEvent);

                entity.Comp.Mode = PaperAction.Write;
                _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
                UpdateUserInterface(entity);
            }
            args.Handled = true;
            return;
        }

        // If a stamp, attempt to stamp paper
        if (TryComp<StampComponent>(args.Used, out var stampComp) && TryStamp(entity, GetStampInfo(stampComp), stampComp.StampState))
        {
            // successfully stamped, play popup
            var stampPaperOtherMessage = Loc.GetString("paper-component-action-stamp-paper-other",
                    ("user", args.User),
                    ("target", args.Target),
                    ("stamp", args.Used));

            _popupSystem.PopupEntity(stampPaperOtherMessage, args.User, Filter.PvsExcept(args.User, entityManager: EntityManager), true);
            var stampPaperSelfMessage = Loc.GetString("paper-component-action-stamp-paper-self",
                    ("target", args.Target),
                    ("stamp", args.Used));
            _popupSystem.PopupClient(stampPaperSelfMessage, args.User, args.User);

            _audio.PlayPredicted(stampComp.Sound, entity, args.User);

            UpdateUserInterface(entity);
        }
    }

    private static StampDisplayInfo GetStampInfo(StampComponent stamp)
    {
        return new StampDisplayInfo
        {
            StampedName = stamp.StampedName,
            StampedColor = stamp.StampedColor
        };
    }

    private void OnInputTextMessage(Entity<PaperComponent> entity, ref PaperInputTextMessage args)
    {
        var ev = new PaperWriteAttemptEvent(entity.Owner);
        RaiseLocalEvent(args.Actor, ref ev);
        if (ev.Cancelled)
            return;

        // Fire edit start - normalize text once and preserve authored content for knowledge-gated paper reading
        var normalizedText = NormalizePaperContent(args.Text);
        if (normalizedText.Length <= entity.Comp.ContentSize)
        {
            SetContent(entity, normalizedText, args.Actor);

            var paperStatus = string.IsNullOrWhiteSpace(normalizedText) ? PaperStatus.Blank : PaperStatus.Written;

            if (TryComp<AppearanceComponent>(entity, out var appearance))
                _appearance.SetData(entity, PaperVisuals.Status, paperStatus, appearance);

            if (TryComp(entity, out MetaDataComponent? meta))
                _metaSystem.SetEntityDescription(entity, "", meta);

            _adminLogger.Add(LogType.Chat,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has written on {ToPrettyString(entity):entity} the following text: {normalizedText}");

            _audio.PlayPvs(entity.Comp.Sound, entity);
        }
        // Fire edit end

        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
    }

    private void OnRandomPaperContentMapInit(Entity<RandomPaperContentComponent> ent, ref MapInitEvent args)
    {
        if (!_paperQuery.TryComp(ent, out var paperComp))
        {
            Log.Warning($"{ToPrettyString(ent)} has a {nameof(RandomPaperContentComponent)} but no {nameof(PaperComponent)}!");
            RemCompDeferred(ent, ent.Comp);
            return;
        }
        var dataset = _protoMan.Index(ent.Comp.Dataset);
        // Intentionally not using the Pick overload that directly takes a LocalizedDataset,
        // because we want to get multiple attributes from the same pick.
        var pick = _random.Pick(dataset.Values);

        // Name
        _metaSystem.SetEntityName(ent, Loc.GetString(pick));
        // Description
        _metaSystem.SetEntityDescription(ent, Loc.GetString($"{pick}.desc"));
        // Content
        SetContent((ent, paperComp), Loc.GetString($"{pick}.content"));

        // Our work here is done
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnPaperWrite(Entity<ActivateOnPaperOpenedComponent> entity, ref PaperWriteEvent args)
    {
        _interaction.UseInHandInteraction(args.User, entity);
    }

    /// <summary>
    ///     Accepts the name and state to be stamped onto the paper, returns true if successful.
    /// </summary>
    public bool TryStamp(Entity<PaperComponent> entity, StampDisplayInfo stampInfo, string spriteStampState)
    {
        if (!entity.Comp.StampedBy.Contains(stampInfo))
        {
            entity.Comp.StampedBy.Add(stampInfo);
            Dirty(entity);
            if (entity.Comp.StampState == null && TryComp<AppearanceComponent>(entity, out var appearance))
            {
                entity.Comp.StampState = spriteStampState;
                // Would be nice to be able to display multiple sprites on the paper
                // but most of the existing images overlap
                _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
            }
        }
        return true;
    }

    /// <summary>
    ///     Copy any stamp information from one piece of paper to another.
    /// </summary>
    public void CopyStamps(Entity<PaperComponent?> source, Entity<PaperComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        target.Comp.StampedBy = new List<StampDisplayInfo>(source.Comp.StampedBy);
        target.Comp.StampState = source.Comp.StampState;
        Dirty(target);

        if (TryComp<AppearanceComponent>(target, out var appearance))
        {
            // delete any stamps if the stamp state is null
            _appearance.SetData(target, PaperVisuals.Stamp, target.Comp.StampState ?? "", appearance);
        }
    }

    public void SetContent(EntityUid entity, string content, EntityUid? author = null) // Fire edit - allow callers to record paper authorship for knowledge gating
    {
        if (!TryComp<PaperComponent>(entity, out var paper))
            return;
        SetContent((entity, paper), content, author);
    }

    public void SetContent(Entity<PaperComponent> entity, string content, EntityUid? author = null) // Fire edit - propagate paper authorship through shared content updates
    {
        // Fire added start - normalize content before author-range tracking so stored offsets stay stable
        content = NormalizePaperContent(content);
        UpdateKnowledgeAuthorRanges(entity, content, author);
        // Fire added end
        entity.Comp.Content = content;
        Dirty(entity);
        UpdateUserInterface(entity);

        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        var status = string.IsNullOrWhiteSpace(content)
            ? PaperStatus.Blank
            : PaperStatus.Written;

        _appearance.SetData(entity, PaperVisuals.Status, status, appearance);
    }

    // Fire added start - preserve paper authorship for SCP knowledge paper sources
    private static void UpdateKnowledgeAuthorRanges(Entity<PaperComponent> entity, string newContent, EntityUid? author)
    {
        if (string.Equals(entity.Comp.Content, newContent, StringComparison.Ordinal))
            return;

        if (author == null)
        {
            ResetKnowledgeAuthorRanges(entity.Comp.KnowledgeAuthorRanges, newContent, null);
            return;
        }

        var oldContent = entity.Comp.Content;
        if (oldContent.Length == 0)
        {
            ResetKnowledgeAuthorRanges(entity.Comp.KnowledgeAuthorRanges, newContent, author);
            return;
        }

        var oldAuthors = BuildKnowledgeAuthorMap(oldContent.Length, entity.Comp.KnowledgeAuthorRanges);
        var newAuthors = new EntityUid?[newContent.Length];
        Array.Fill(newAuthors, author);

        CopyUnchangedKnowledgeAuthors(oldContent, newContent, oldAuthors, newAuthors);
        WriteKnowledgeAuthorRanges(entity.Comp.KnowledgeAuthorRanges, newAuthors);
    }

    private static void ResetKnowledgeAuthorRanges(
        List<PaperKnowledgeAuthorRange> ranges,
        string content,
        EntityUid? author)
    {
        ranges.Clear();

        if (content.Length > 0)
            ranges.Add(new PaperKnowledgeAuthorRange(0, content.Length, author));
    }

    private static EntityUid?[] BuildKnowledgeAuthorMap(int length, List<PaperKnowledgeAuthorRange> ranges)
    {
        var authors = new EntityUid?[length];
        if (length == 0)
            return authors;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            if (range.Length <= 0)
                continue;

            var start = Math.Clamp(range.Start, 0, length);
            var end = Math.Clamp(range.End, start, length);
            for (var index = start; index < end; index++)
            {
                authors[index] = range.Author;
            }
        }

        return authors;
    }

    private static void CopyUnchangedKnowledgeAuthors(
        string oldContent,
        string newContent,
        EntityUid?[] oldAuthors,
        EntityUid?[] newAuthors)
    {
        var prefixLength = GetCommonPrefixLength(oldContent, newContent);
        for (var i = 0; i < prefixLength; i++)
        {
            newAuthors[i] = oldAuthors[i];
        }

        var suffixLength = GetCommonSuffixLength(oldContent, newContent, prefixLength);
        for (var i = 0; i < suffixLength; i++)
        {
            var oldIndex = oldContent.Length - suffixLength + i;
            var newIndex = newContent.Length - suffixLength + i;
            newAuthors[newIndex] = oldAuthors[oldIndex];
        }

        var oldMiddleStart = prefixLength;
        var oldMiddleLength = oldContent.Length - prefixLength - suffixLength;
        var newMiddleStart = prefixLength;
        var newMiddleLength = newContent.Length - prefixLength - suffixLength;

        if (oldMiddleLength <= 0 || newMiddleLength <= 0)
            return;

        const long maxExactDiffArea = 4_000_000;
        if ((long) oldMiddleLength * newMiddleLength > maxExactDiffArea)
            return;

        var lcs = new int[oldMiddleLength + 1, newMiddleLength + 1];
        for (var oldIndex = 1; oldIndex <= oldMiddleLength; oldIndex++)
        {
            var oldCharacter = oldContent[oldMiddleStart + oldIndex - 1];
            for (var newIndex = 1; newIndex <= newMiddleLength; newIndex++)
            {
                if (oldCharacter == newContent[newMiddleStart + newIndex - 1])
                {
                    lcs[oldIndex, newIndex] = lcs[oldIndex - 1, newIndex - 1] + 1;
                    continue;
                }

                lcs[oldIndex, newIndex] = Math.Max(lcs[oldIndex - 1, newIndex], lcs[oldIndex, newIndex - 1]);
            }
        }

        var oldCursor = oldMiddleLength;
        var newCursor = newMiddleLength;

        while (oldCursor > 0 && newCursor > 0)
        {
            var oldIndex = oldMiddleStart + oldCursor - 1;
            var newIndex = newMiddleStart + newCursor - 1;

            if (oldContent[oldIndex] == newContent[newIndex])
            {
                newAuthors[newIndex] = oldAuthors[oldIndex];
                oldCursor--;
                newCursor--;
                continue;
            }

            if (lcs[oldCursor - 1, newCursor] >= lcs[oldCursor, newCursor - 1])
            {
                oldCursor--;
            }
            else
            {
                newCursor--;
            }
        }
    }

    private static void WriteKnowledgeAuthorRanges(List<PaperKnowledgeAuthorRange> ranges, EntityUid?[] authors)
    {
        ranges.Clear();
        if (authors.Length == 0)
            return;

        var currentAuthor = authors[0];
        var currentStart = 0;

        for (var i = 1; i < authors.Length; i++)
        {
            if (authors[i] == currentAuthor)
                continue;

            ranges.Add(new PaperKnowledgeAuthorRange(currentStart, i - currentStart, currentAuthor));
            currentStart = i;
            currentAuthor = authors[i];
        }

        ranges.Add(new PaperKnowledgeAuthorRange(currentStart, authors.Length - currentStart, currentAuthor));
    }

    private static string NormalizePaperContent(string content)
    {
        if (content.Length == 0)
            return content;

        return content.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static int GetCommonPrefixLength(string left, string right)
    {
        var limit = Math.Min(left.Length, right.Length);
        var index = 0;

        while (index < limit && left[index] == right[index])
        {
            index++;
        }

        return index;
    }

    private static int GetCommonSuffixLength(string left, string right, int prefixLength)
    {
        var suffixLength = 0;
        var suffixLimit = Math.Min(left.Length - prefixLength, right.Length - prefixLength);

        while (suffixLength < suffixLimit &&
               left[left.Length - suffixLength - 1] == right[right.Length - suffixLength - 1])
        {
            suffixLength++;
        }

        return suffixLength;
    }

    // Fire added end

    // Sunrise-Start
    public void SetImageContent(Entity<PaperComponent> entity, SpriteSpecifier content, Vector2? scale = null)
    {
        entity.Comp.ImageContent = content;
        entity.Comp.ImageScale = scale;
        Dirty(entity);
        UpdateUserInterface(entity);
    }
    // Sunrise-End

    private void UpdateUserInterface(Entity<PaperComponent> entity)
    {
        // Fire added start - для визуала дневника
        var ev = new PaperWrittenEvent();
        RaiseLocalEvent(entity, ref ev);
        // Fire added end

        _uiSystem.SetUiState(entity.Owner, PaperUiKey.Key, new PaperBoundUserInterfaceState(entity.Comp.Content, entity.Comp.DefaultColor, entity.Comp.StampedBy, entity.Comp.Mode, entity.Comp.ImageContent, entity.Comp.ImageScale)); // Sunrise-edit
    }
}

/// <summary>
/// Event fired when using a pen on paper, opening the UI.
/// </summary>
[ByRefEvent]
public record struct PaperWriteEvent(EntityUid User, EntityUid Paper);

/// <summary>
/// Cancellable event for attempting to write on a piece of paper.
/// </summary>
/// <param name="paper">The paper that the writing will take place on.</param>
[ByRefEvent]
public record struct PaperWriteAttemptEvent(EntityUid Paper, string? FailReason = null, bool Cancelled = false);

// Fire added start - для визуала дневника
[ByRefEvent]
public record struct PaperWrittenEvent;
// Fire added end
