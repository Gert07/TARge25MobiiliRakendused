# Itaalia köögi meistriklass

Ava lahenduses **CarouselView** projekt ja määra see käivitusprojektiks. Käivitamisel avaneb kohe Itaalia köögi galerii.

- Viis kohalikku toidupilti: carbonara, Margherita, tiramisu, safranirisoto ja lasanje.
- `CarouselView` kasutab korduvkasutatavat, tüübistatud `DataTemplate`-i ja `Dish`-andmemudelit.
- `IndicatorView` on seotud karusselliga. Kaarti saab vahetada pühkimisega või automaatse slaidiesitusega.
- Neljasekundiline taimer arvestab käsitsi valitud asukohta ning liigub viimaselt roalt esimesele.
- **Peata/Jätka** lubab slaidiesitust juhtida. Detailakna või lipumenüü avamise ajal, lehelt lahkudes ja rakenduse taustale minnes taimer peatub.
- Kaardi või **Tutvu roaga** nupu vajutus avab `DisplayAlertAsync`-akna koostisosade, valmistamisaja ja nipiga.
- Keeled: eesti, inglise ja itaalia. Keele valimiseks ava aktiivse lipu ja allanoolega rippmenüü ning vajuta soovitud lipule. Tõlked asuvad failides `Resources/Localization/KitchenResources.et.resx`, `KitchenResources.resx` ja `KitchenResources.it.resx`. Neutraalne ressurss on ingliskeelne.
- Keele vahetamine uuendab olemasolevaid kaarte ja kasutajaliidest kohe; valik salvestatakse `Preferences` abil võtmega `Kitchen.Language`.
- Kujunduses on säilinud päikesekollane ja taevasinine gradient, tomatipunased nupud ning kuldsed kaardiservad. Lihtsustatud vaates on suurem ja kontrastsem tekst; tervitusplokk, piirkonnanimed, noolenupud, loendur ning jalus on eemaldatud.

## Koodi ülesehitus

- `MainPage.xaml`: kujundus ja kaartide mall.
- `MainPage.xaml.cs`: taimer, elutsükkel, navigeerimine ja detailaknad.
- `Kitchen/Models/Dish.cs`: roa andmemudel.
- `Kitchen/ViewModels/KitchenViewModel.cs`: andmestik, keelevalik ja kuvamisolek.
- `Kitchen/Localization/KitchenLocalizer.cs`: .resx ResourceManager ja keelemuutuse teavitused.

## Kontrollimine

```powershell
dotnet build CarouselView.csproj -f net10.0-windows10.0.19041.0 --no-restore
dotnet build CarouselView.csproj -f net10.0-android --no-restore
```

Käsitsi kontrollimiseks:
1. Ava galerii, oota neli sekundit ja kontrolli aktiivse täpi muutumist.
2. Liigu käsitsi kolmandale kaardile ja oota: järgmisena peab avanema neljas kaart.
3. Liigu viimasele kaardile ja kontrolli automaatset jätkamist esimeselt kaardilt.
4. Peata slaidiesitus; kaart peab jääma paigale. Jätka.
5. Ava detailid kaardi ja nupu kaudu. Hoia aken avatuna üle nelja sekundi; taustal ei tohi kaart vahetuda.
6. Ava lipumenüü ja vali kordamööda Eesti, Ühendkuningriigi ning Itaalia lipp. Kontrolli pealkirja, kirjeldust, detaile ja juhtnuppe. Taaskäivita rakendus ja kontrolli keele säilimist.
7. Lahku lehelt või vii rakendus taustale. Naasmisel peab slaidiesitus jätkuma ainult siis, kui see oli sisse lülitatud.

## Genereeritud pildid

Pildid loodi sisseehitatud **image_gen** tööriistaga ja kopeeriti projekti.
Kõik on 1536 × 1024 PNG-failid; rakendus kasutab neid võrguühenduseta.
Algseid genereeritud faile ei kustutatud.

Lõplikud failid:
- `Resources/Images/kitchen_carbonara.png`
- `Resources/Images/kitchen_margherita.png`
- `Resources/Images/kitchen_tiramisu.png`
- `Resources/Images/kitchen_risotto.png`
- `Resources/Images/kitchen_lasagne.png`

Iga pildi lõplik prompt kasutab järgmist ühist teksti, asendades `{subject}` alloleva vastava rea väärtusega:

```text
Use case: photorealistic-natural
Asset type: local food photograph for a .NET MAUI Italian cooking class carousel.
Subject: {subject}
Style: appetizing high-end editorial food photography, realistic ingredients and textures.
Scene: warm rustic Italian kitchen table, subtle cream linen and dark olive background.
Composition: landscape 3:2 photograph, close slightly overhead angle, single complete serving centered, enough margin for a mobile card crop.
Lighting: soft natural window light, warm highlights, gentle shadows.
Constraints: one photograph, no collage, no people, no text, no logos, no watermark.
```

### carbonara

```text
Pasta Carbonara: spaghetti coated with glossy egg and pecorino sauce, crisp guanciale pieces, cracked black pepper, in a shallow ivory ceramic bowl. No cream, no parsley.
```

### margherita

```text
Pizza Margherita: whole authentic Neapolitan pizza, blistered puffy crust, red tomato sauce, melted white mozzarella and fresh green basil, on a ceramic plate.
```

### tiramisu

```text
Tiramisu: one elegant square slice with clearly visible alternating mascarpone cream and coffee soaked ladyfinger layers, cocoa dusting on top, on a small ivory ceramic plate.
```

### risotto

```text
Risotto alla Milanese: creamy golden saffron rice with distinct rice grains, a light shaving of parmesan, in a shallow ivory ceramic bowl.
```

### lasagne

```text
Lasagne alla Bolognese: a generous square portion with visible pasta, slow cooked meat ragu and bechamel layers, golden baked top, on an ivory ceramic plate.
```

Lippude genereerimise promptid ja pildifailid: [Kitchen/FlagImages.md](Kitchen/FlagImages.md).

Varem loodud kokamaskoti pilt ja prompt on säilitatud failis [Kitchen/Mascot.md](Kitchen/Mascot.md); maskotti praegusel avalehel ei kuvata.
