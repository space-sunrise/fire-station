# Playback Commands

cmd-replay-play-desc = Reanuda la reproducción.
cmd-replay-play-help = Uso: replay_play
cmd-replay-pause-desc = Pone la reproducción.
cmd-replay-pause-help = Uso: replay_pause
cmd-replay-toggle-desc = Reanuda o pausa la reproducción.
cmd-replay-toggle-help = Uso: replay_toggle
cmd-replay-stop-desc = Detiene y descarga la reproducción.
cmd-replay-stop-help = Uso: replay_stop
cmd-replay-load-desc = Carga y comienza la reproducción.
cmd-replay-load-help = Uso: carpeta replay_load <replay>
cmd-replay-load-hint = Carpeta de reproducción
cmd-replay-skip-desc = Avanza o rebobina en el tiempo.
cmd-replay-skip-help = Uso: replay_skip <tic o periodo de tiempo>
cmd-replay-skip-hint = Tics o intervalo de tiempo (HH:MM:SS).
cmd-replay-set-time-desc = Avanza o retrocede hasta un tiempo específico.
cmd-replay-set-time-help = Uso: replay_set <tic o hora>
cmd-replay-set-time-hint = Tic o intervalo de tiempo (HH:MM:SS), comenzando desde
cmd-replay-error-time = "{ $time }" no es un entero ni un intervalo de tiempo.
cmd-replay-error-args = Número incorrecto de argumentos.
cmd-replay-error-no-replay = La reproducción no está activa en este momento.
cmd-replay-error-already-loaded = La reproducción ya está descargada.
cmd-replay-error-run-level = No puedes descargar la reproducción mientras estás conectado al servidor.

# Recording commands

cmd-replay-recording-start-desc = Empieza a grabar la reproducción, posiblemente con un límite de tiempo.
cmd-replay-recording-start-help = Uso: replay_recording_start [nombre] [sobrescritura] [límite de tiempo]
cmd-replay-recording-start-success = La grabación de reproducción ha comenzado.
cmd-replay-recording-start-already-recording = La grabación de la reproducción ya está en marcha.
cmd-replay-recording-start-error = Ocurrió un error al intentar iniciar la grabación.
cmd-replay-recording-start-hint-time = [time limit (minutes)]
cmd-replay-recording-start-hint-name = [name]
cmd-replay-recording-start-hint-overwrite = [overwrite (bool)]
cmd-replay-recording-stop-desc = Detiene la grabación de la reproducción.
cmd-replay-recording-stop-help = Uso: replay_recording_stop
cmd-replay-recording-stop-success = La grabación de reproducción se ha detenido.
cmd-replay-recording-stop-not-recording = Actualmente no se está grabando ninguna reproducción.
cmd-replay-recording-stats-desc = Muestra información sobre la grabación de reproducción actual.
cmd-replay-recording-stats-help = Uso: replay_recording_stats
cmd-replay-recording-stats-result = Duración: { $time } min, Ticks: { $ticks }, Tamaño: { $size } MB, Velocidad: { $rate } MB/min
# Time Control UI
replay-time-box-scrubbing-label = Rebobinado dinámico
replay-time-box-replay-time-label = Tiempo de grabación: { $current } / { $end } ({ $percentage }%)
replay-time-box-server-time-label = Tiempo del servidor: { $current } / { $end }
replay-time-box-index-label = Índice: { $current } / { $total }
replay-time-box-tick-label = Teca: { $current } / { $total }
