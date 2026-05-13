scp-radio-cycle-channel = Cycle Channel
scp-radio-toggle-radio = Toggle On/Off
scp-radio-current-channel = Current channel is now: { $name }
scp-radio-microphone =
    Microphone { $value ->
        [true] on
        *[false] off
    }
scp-radio-radio-status =
    Radio: { $value ->
      [true] [bold]on[/bold]
      *[false] [bold]off[/bold]
    }
scp-radio-microphone-status =
    Microphone: { $value ->
      [true] [bold]on[/bold]
      *[false] [bold]off[/bold]
    }
scp-radio-not-enough-charge = Insufficient charge
scp-radio-toggle-message =
    { $name } is { $value ->
      [true] turning on
      *[false] turning off
    }
