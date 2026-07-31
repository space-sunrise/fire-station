lathe-menu-title = Menú de máquina
lathe-menu-queue = Cola
lathe-menu-server-list = Lista de servidores
lathe-menu-sync = Synchr.
lathe-menu-search-designs = Búsqueda de proyectos
lathe-menu-category-all = Eso es todo
lathe-menu-search-filter = Filtro
lathe-menu-amount = Resumen:
lathe-menu-recipe-count =
    { $count ->
        [1] { $count } Recipe
       *[other] { $count } Recipes
    }
lathe-menu-reagent-slot-examine = Hay un agujero para un vaso de precipitados en el lateral.
lathe-reagent-dispense-no-container = ¡El líquido se vierte del { $name } al suelo!
lathe-menu-result-reagent-display = { $reagent } ({ $amount }units)
lathe-menu-material-display = { $material } { $amount }
lathe-menu-tooltip-display = { $amount } { $material }
lathe-menu-description-display = [italic]{ $description }[/italic]
lathe-menu-material-amount =
    { $amount ->
        [1] { NATURALFIXED($amount, 2) } ({ $unit })
       *[other] { NATURALFIXED($amount, 2) } ({ $unit })
    }
lathe-menu-material-amount-missing =
    { $amount ->
        [1] { NATURALFIXED($amount, 2) } { $unit } { $material } ([color=red]{ NATURALFIXED($missingAmount, 2) } { $unit } es missing[/color])
       *[other] { NATURALFIXED($amount, 2) } { $unit } { $material } ([color=red]{ NATURALFIXED($missingAmount, 2) } { $unit } es missing[/color])
    }
lathe-menu-no-materials-message = Materiales no subidos
lathe-menu-fabricating-message = Producido...
lathe-menu-materials-title = Materiales
lathe-menu-queue-title = Cola de producción
