namespace Content.Server._Scp.Other.SpeakOnEyeStateChange;

/// <summary>
/// Компонент, отвечающий за проговаривание заданных фраз при изменении состояния глаз(открытие/закрытие).
/// Может быть настроено, чтобы говорить только внутри камеры содержания SCP-173 и рядом с ним.
/// </summary>
[RegisterComponent]
public sealed partial class SpeakOnEyeStateChangeComponent : Component
{
    /// <summary>
    /// Список фраз, которые будет говорить персонаж при закрытии глаз
    /// </summary>
    [DataField]
    public List<string>? PhraseOnClose;

    /// <summary>
    /// Список фраз, которые будет говорить персонаж при открытии глаз
    /// </summary>
    [DataField]
    public List<string>? PhraseOnOpen;

    /// <summary>
    /// Любимая фраза при открытии глаз. Персонаж чаще будет говорить именно ее.
    /// Выбирается при спавне сущности.
    /// </summary>
    [ViewVariables]
    public string FavoriteOpenPhrase;

    /// <summary>
    /// Любимая фраза при закрытии глаз. Персонаж чаще будет говорить именно ее.
    /// Выбирается при спавне сущности.
    /// </summary>
    [ViewVariables]
    public string FavoriteClosePhrase;

    /// <summary>
    /// Шанс, что персонаж скажет свою любимую фразу.
    /// </summary>
    [DataField]
    public float FavoriteChance = 0.9f;

    /// <summary>
    /// Следует ли говорить фразы только внутри камеры содержания SCP-173?
    /// </summary>
    [DataField]
    public bool SpeakOnlyInScp173Chamber = true;

    /// <summary>
    /// Следует ли говорить фразы только рядом с SCP-173?
    /// </summary>
    [DataField]
    public bool SpeakOnlyWhileScp173Nearby = true;
}
