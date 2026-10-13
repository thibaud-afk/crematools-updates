# Sources de CrémaTools

Application Windows native WPF/.NET 10, distribuée sous licence MIT. Les moteurs tiers conservent leurs licences respectives. Le logo CremaLab identifie la version officielle ; pour une version modifiée, utilisez votre propre nom et identité visuelle.

## Compiler

Prérequis de développement : Windows x64, [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), Python 3 et [NSIS 3](https://nsis.sourceforge.io/Download). Ces outils ne sont **pas nécessaires pour utiliser le logiciel installé**.

Depuis ce dossier :

```powershell
dotnet restore --packages .nuget/packages
python tools/fetch-heif.py
Invoke-WebRequest https://www.7-zip.org/a/7zr.exe -OutFile assets/7zr.exe
dotnet build -c Release
dotnet run -c Release -- --self-test
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

Les tests utilisent un profil isolé, des documents fictifs et une mise à jour fictive qui n'est jamais exécutée. Le rapport et les captures sont dans `test-results` à côté de l'exécutable.

Pour produire l'installateur, exécutez `build-installer.ps1` après la publication. Ajoutez NSIS à votre PATH ou fournissez son chemin au paramètre `-MakeNsis`.

```powershell
./build-installer.ps1 -MakeNsis 'C:/Program Files (x86)/NSIS/makensis.exe'
```

Les moteurs HEIC sont téléchargés depuis les paquets officiels MSYS2, avec contrôle SHA-256. Le script ne les installe pas dans Windows : il assemble les fichiers nécessaires dans `assets/heif`. Le script `tools/package-native-sources.py` prépare leurs sources correspondantes pour accompagner une redistribution.

## Organisation

- `MainWindow*`, `App.xaml` : interface et charte visuelle.
- `FileEngine.cs` : conversion, compression, ZIP/7z.
- `ScannerEngine.cs` : OCR Windows, Word, PDF recherchable.
- `RenameEngine.cs` : aperçu, déplacement en deux phases et annulation.
- `VaultEngine.cs` : chiffrement, récupération, sauvegarde et restauration.
- `UpdateEngine.cs` : validation des manifestes signés et téléchargement.
- `SelfTests.cs`, `UpdateChecks.cs`, `UiChecks.cs` : vérifications fonctionnelles et captures.

## Coffre et sécurité

AES-256-GCM avec nonce aléatoire par fichier ; clé de coffre aléatoire enveloppée par une clé dérivée de la phrase secrète via PBKDF2-HMAC-SHA256 (600 000 itérations), et séparément par une clé de récupération aléatoire de 256 bits. Noms et catégories sont dans un index chiffré. Les sauvegardes contiennent les fichiers chiffrés et restent portables entre ordinateurs.

Ce descriptif ne remplace pas un audit indépendant. Ne joignez jamais de documents privés, de phrase secrète ou de clé de récupération dans une issue publique.

## Mises à jour et versions modifiées

La clé **publique** et un manifeste de test signé sont inclus. La clé privée officielle n'est jamais publiée. Le script de signature nécessite une clé privée détenue par le mainteneur ; elle est volontairement absente des sources.

Pour distribuer une version modifiée, générez votre propre paire de clés RSA, remplacez la clé publique et le manifeste de test, puis configurez votre propre adresse de mise à jour. Ne conservez pas l'adresse officielle pour un fork : une mise à jour officielle pourrait remplacer votre version modifiée.

La signature du manifeste RSA-PSS/SHA-256 ne constitue pas un certificat Authenticode de l'installateur Windows.
