# Alien Breach — Tower Defense

## Lühikirjeldus

**Alien Breach** on C# ja .NET 10 MAUI abil loodud tornikaitsemäng Androidile ja Windowsile. Mängija kaitseb baasi järjest tugevamate tulnukalainete eest, paigutades kaardile automaatselt tulistavaid torne.
Valikus on kolm kaarti ja viis tornitüüpi, mida saab missiooni jooksul parandada või müüa. Eesmärk on elada üle võimalikult palju laineid. Mäng toetab eesti ja inglise keelt ning heliefekte; 
tulemused, rekordid ja sätted salvestatakse kohalikku SQLite'i andmebaasi.

## Kasutatud arhitektuur

Rakendus kasutab **kihilist objektorienteeritud arhitektuuri**, kus mänguloogika on kasutajaliidesest eraldatud. **MVVM-mustrit kasutatakse osaliselt**: sätete lehel on eraldi vaatemudel, 
kuid mängulehtede sisend ja navigeerimine on juhitud lehtede C# koodis.

- **`AlienBreach.Core`** sisaldab kasutajaliidesest sõltumatut mängumootorit, vastaseid, torne, kaarte ja andmemudeleid. Tornide ning vastaste ühine käitumine on lahendatud pärimise ja polümorfismiga.
- **`AlienBreach.Mobile`** sisaldab MAUI kasutajaliidest. `Views/` hoiab lehti, `ViewModels/` vaatemudeleid, `Models/` kasutajaliidese andmemudeleid ning `Services/` seadete, tõlgete ja heli teenuseid.
 Mängugraafika joonistatakse `GraphicsView` kaudu.
- **Andmesalvestus kasutab Repository-mustrit**: `IGameRepository` määrab andmeoperatsioonid ning `SqliteGameRepository` teostab need SQLite'is. 
Sõltuvused registreeritakse `MauiProgram` klassis ja antakse komponentidele konstruktorite kaudu. Mängumootor edastab heli- ja lõpusündmusi sündmuste abil.

## Kolm kaarti

| Map | Route and strategy |
| **Outpost 07** | Standard. A cargo-lane switchback with several shared bends; overlap firing ranges |
| **Reactor Ring** | Forgiving. A long spiral into the reactor chamber; multiple passes through firing zones |
| **Cryo Crossing** | Challenging. A short diagonal ice corridor; use slows and railgun range to cover the crossings | 

## Viis torni

| Turret | Cost | Starting range | Starting damage / interval | Role |
| **Pulse turret** | $90 | 165 | 18 / 0.45 s | Affordable, balanced single-target fire |
| **Rapid repeater** | $150 | 150 | 8 / 0.14 s | Fast fire against light packs |
| **Plasma cannon** | $180 | 190 | 48 / 1.20 s | Splash damage within 75 world units |
| **Cryo emitter** | $120 | 155 | 8 / 0.60 s | Slows the target by 55% for 2.5 seconds |
| **Railgun** | $240 | 330 | 115 / 1.65 s | Targets the strongest alien and pierces up to three in a line |

## OOP struktuur

```text
src/AlienBreach.Core/
  Entities/       Entity → LivingEntity → Alien → Stalker, Spitter, Brute, RedTitan
                  Projectile and Impact
  Turrets/        Turret → PulseTurret, RepeaterTurret, PlasmaTurret, CryoTurret, RailgunTurret
                  ITargetingStrategy → FirstTargeting, StrongestTargeting
                  TurretCatalog and TurretFactory
  Maps/           BattleMap, MapCatalog, EnemyPath, MapViewport
  Game/           GameEngine, WaveDirector, EnemyFactory, EnemyDifficulty, SoundCue
  Models/         PlayerProfile, RunResult, MapRecord, GameSettings
  Persistence/    IGameRepository → SqliteGameRepository
src/AlienBreach.Mobile/
  Models/         LanguageOption (keelevaliku andmed)
  ViewModels/     SettingsViewModel (sätete muutmine, salvestusolek ja vead)
  Views/          HudPage, StartupPage, HangarPage, SettingsPage, MapSelectionPage,
                  MapDetailsPage, RecordsPage, HistoryPage, MissionPage
  Services/       SettingsService (SQLite'i sätted), L (RESX-i tõlked ja kultuur)
                  GameAudioService, IAudioPlayback, PlatformAudioPlayback, SoundGenerator
  Resources/
    Localization/ AppResources.resx, AppResources.et.resx
    Images/       Rakenduse pildifailid (mängugraafika joonistatakse ka lõuendile)
  Rendering/      GameRenderer, HudBackdrop
  Controls/       TurretIconButton
  PlatformUI/     GameScreenMode
tests/AlienBreach.Tests/
                  Gameplay, map, alien, economy, SQLite, and migration tests
```