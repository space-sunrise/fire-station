## SuspicionGui.xaml.cs

# Shown when clicking your Role Button in Suspicion
suspicion-ally-count-display =
    { $allyCount ->
       *[zero] Estás solo. ¡Suerte!
        [one] Tu aliado: { $allyNames }.
        [other] Tus aliados: { $allyNames }.
    }
