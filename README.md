# ProjectHub

Bibliothèque Windows native de projets locaux : C# / .NET 10, WinUI 3 / Windows App SDK, SQLite et MVVM.

## Lancer

Ouvrir `artifacts/ProjectHub-win-x64/ProjectHub.App.exe` après publication, ou exécuter `Lancer-ProjectHub.ps1`. Le dossier portable contient .NET et Windows App SDK. Conserver ses fichiers ensemble.

Ajouter un ou plusieurs dossiers racines puis **Scanner**. Aucun disque entier n'est parcouru par défaut. La recherche accepte `language:rust`, `framework:godot`, `git:dirty`, `tag:game`, `favorite:true`, `modified:<30d`, `size:>1gb` et des mots ordinaires. Ctrl+K ouvre la palette ; Ctrl+Alt+P ouvre la recherche globale.

L’interface est disponible en français et en anglais. Choisissez la langue dans **Paramètres → Apparence** ; le changement s’applique immédiatement.

Les détails regroupent statistiques, Git, README, TODO, stockage, composants, notes et commandes. Une commande ne s'exécute qu'après un clic dans **Lancer**. Sa sortie et son arrêt sont accessibles dans **Tâches**. Les favoris, collections et notes restent hors des repositories.

La configuration et l'index se trouvent dans `%LOCALAPPDATA%/ProjectHub`. Pour isoler une session de test, définir `PROJECTHUB_DATA_DIR` avant le lancement. Les logs tournent sur quatre fichiers de 2 Mo.

## Développer

```powershell
dotnet build ProjectHub.slnx -c Release
dotnet test tests/ProjectHub.Tests -c Release
./tools/Publish.ps1
```

Le SDK .NET 10 et le SDK Windows 10.0.26100 sont nécessaires pour compiler. La solution sépare le moteur portable `ProjectHub.Core` de l'interface native `ProjectHub.App`. Ajouter un détecteur via `IProjectDetector` et `DetectorRegistry`.

## Mesures et limites

Les lignes et commentaires sont classifiés heuristiquement. Les fichiers de code de plus de 2 Mo, les fichiers minifiés et les répertoires exclus ne sont pas indexés comme source. L'analyse disque inclut les dépendances, sans suivre les liens symboliques. Le budget d'analyse par projet est configurable ; une mesure interrompue par une limite est indiquée comme partielle. Les événements commencent à l'installation et restent limités à 250 par projet.

Les repos imbriqués indépendants sont séparés ; les solutions, workspaces et applications regroupent leurs composants. Les exemples et fixtures d'un projet ne contribuent pas à sa stack principale. Les manifestes restent des données. Git ne fait ni fetch, ni checkout, ni commit, et ses hooks/fsmonitor sont désactivés pour la lecture.

Le nettoyage expose uniquement des dossiers régénérables immédiats du projet, refuse les liens et passe par la confirmation Windows puis la corbeille. Le stockage source est toujours conservé. Les commandes batch avec caractères shell particuliers sont refusées ; elles restent exécutables manuellement dans un terminal.

Cette première version ne reconstitue pas les variations historiques de lignes, ne vérifie pas la fraîcheur des dépendances en ligne et n'intègre pas encore l'API GitHub. Le README est rendu avec les éléments Markdown usuels en contrôles natifs, sans HTML ou WebView.
