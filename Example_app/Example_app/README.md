# Example_app

.NET MAUI näidisrakendus koos trips-traps-trulli mänguga.

## Kaustastruktuur

```text
Models/                  Andmemudelid: AppSettings ja GameStats
ViewModels/              Seadete kuva olek ja sidumised (SettingsViewModel)
Views/                   Avaleht ja sätete leht
  Game/                  Mäng, reeglid ja statistika
  Demos/                 Senised MAUI harjutuste lehed
Services/                SQLite, seadete haldus, tõlked ja mängumootor
Resources/Localization/  Ingliskeelne põhiressurss ja eestikeelsed .resx-tõlked
Resources/Images/        Rakenduse pildid
```

## Millist andmebaasi kasutame?

Kasutame **SQLite'i**, mis salvestab andmed seadmes ühte kohalikku faili.
Ühenduseks on `Microsoft.Data.Sqlite.Core` ja SQLite'i platvormiteegid lisab
`SQLitePCLRaw.bundle_e_sqlite3`. Eraldi serverit ega internetiühendust pole vaja.

Andmebaas asub `Path.Combine(FileSystem.AppDataDirectory, "example_app.db3")`.
Täpne kaust sõltub platvormist. `AppDatabase` loob faili ja tabelid automaatselt.

| Tabel | Salvestatav info |
| --- | --- |
| `AppSettings` | Keel (`et`/`en`), teema (`Light`/`Dark`), mängurežiim (0 = kaks mängijat, 1 = bot), tervitusjuhendi olek |
| `GameStats` | X-i võidud, O võidud ja viigid |

Mõlemas tabelis on üks rida võtmega `Id = 1`.
Seade kirjutatakse kohe muudatuse tegemisel andmebaasi SQL-i `INSERT ... ON CONFLICT DO UPDATE`
käsuga. Väärtused edastatakse SQL-parameetritena. Alles pärast edukat salvestamist rakendub
muudatus kasutajaliideses. Käivitamisel loeb `SettingsService` samad väärtused tagasi;
salvestatud keel ja teema rakenduvad enne esimese lehe näitamist.

Varem hoiti statistikat ja tervitusjuhendi olekut MAUI `Preferences`-salvestis.
See oli võtme-väärtuse salvestus, mitte SQLite-andmebaas. Esimesel käivitamisel
imporditakse need väärtused SQLite'i; järgmistel kordadel olemasolevaid ridu üle ei kirjutata.
Statistika puhastamine ei muuda sätteid. Rakenduse eemaldamine või selle andmete kustutamine
võib eemaldada ka kohaliku andmebaasi. Pooleliolev mängulaud ei salvestu.

## Keelevalik ja sätted

Ava avalehelt või mängust **Sätted / Settings**. Vali **Eesti** või **English**,
hele/tume teema ja mängu vastane. Eraldi salvestusnuppu pole vaja.
Mängus olevad teema- ja vastasevalikud kasutavad sama seadete teenust ning salvestuvad samuti.
Vastase muutmisel algab uus mäng.

Mäng, reeglid, statistika, sätted ja peamenüü on mõlemas keeles.
`Views/Demos` õppematerjalide sisu on jäetud senisesse keelde.
`AppResources.resx` sisaldab ingliskeelseid tekste ja `AppResources.et.resx`
eestikeelseid. `TranslateExtension` seob XAML-i tekstid `LanguageService`-iga,
nii et keele vahetamine uuendab avatud vaate tekste kohe.

SQLite'i kasutamine MAUI-s: [Microsofti dokumentatsioon](https://learn.microsoft.com/dotnet/maui/data-cloud/database-sqlite).
Ühendusteegi ja SQLite'i platvormipakettide seos: [Microsoft.Data.Sqlite dokumentatsioon](https://learn.microsoft.com/dotnet/standard/data/sqlite/custom-versions).

## Kontrollimine

```powershell
dotnet build Example_app.csproj -f net10.0-windows10.0.19041.0
dotnet run --project Tests/StorageChecks/StorageChecks.csproj
```

Salvestuskontrollid kasutavad päris ajutist SQLite-faili ning käivitavad eraldi protsessi,
et kontrollida seadete ja statistika säilimist. Samuti kontrollitakse vana statistika
ülekannet, seadete säilimist statistika nullimisel ja mõlema keele tõlkeid.

Käsitsi kontroll: vali inglise keel, tume teema ja bot; sulge rakendus täielikult ning ava
uuesti. Sätted, mängurežiim ja kasutajaliidese keel peavad olema samad.
