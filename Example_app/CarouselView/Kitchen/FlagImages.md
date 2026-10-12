# Keelevaliku lipupildid

Loodud sisseehitatud **image_gen** tööriistaga. Genereeritud PNG-pildid kopeeriti projekti; algsed failid säilitati.
Kõigil kolmel pildil on läbipaistev taust ning lipu valged osad on läbipaistmatud.

- `Resources/Images/flag_et.png`: Eesti lipp → eesti keel.
- `Resources/Images/flag_en.png`: Ühendkuningriigi lipp → inglise keel.
- `Resources/Images/flag_it.png`: Itaalia lipp → itaalia keel.

MAUI pilditöötlus loob ehitamisel väiksemad platvormipildid (BaseSize 128 × 128).
Kasutajaliides kasutab `AspectFit`-i, et säilitada lipu proportsioonid.

## Estonia — flag_et.png

```text
Use case: logo-brand
Asset type: bitmap flag icon for a language dropdown in a mobile application.
Subject: A flat rectangular Estonian national flag, ratio 11:7, three equal horizontal stripes: blue (#4891D9) at the top, black in the middle, pure white at the bottom.
Style: perfectly flat clean graphic with sharp straight edges and solid uniform colors, front view.
Composition: one centered horizontal flag occupying 90 percent of the canvas width with small transparent margins, preserve the stated flag proportions. Square canvas.
Constraints: transparent background outside the flag; all white parts inside the flag must be opaque white. No text, no letters, no flagpole, no border, no waving, no folds, no texture, no shadow, no bevel, no rounded corners, no other objects.
```

## United Kingdom — flag_en.png

```text
Use case: logo-brand
Asset type: bitmap flag icon for a language dropdown in a mobile application.
Subject: A flat rectangular Union Jack flag of the United Kingdom, ratio 2:1: navy blue field, correct white diagonal Saint Andrew's cross and narrower offset red Saint Patrick's cross, broad central white cross with narrower red Saint George's cross. Correct conventional Union Jack geometry, top left diagonal white stripe broader above the red stripe.
Style: perfectly flat clean graphic with sharp straight edges and solid uniform colors, front view.
Composition: one centered horizontal flag occupying 90 percent of the canvas width with small transparent margins, preserve the stated flag proportions. Square canvas.
Constraints: transparent background outside the flag; all white parts inside the flag must be opaque white. No text, no letters, no flagpole, no border, no waving, no folds, no texture, no shadow, no bevel, no rounded corners, no other objects.
```

## Italy — flag_it.png

```text
Use case: logo-brand
Asset type: bitmap flag icon for a language dropdown in a mobile application.
Subject: A flat rectangular Italian national flag, ratio 3:2, three equal vertical stripes: green (#009246) on the left, pure white in the middle, red (#CE2B37) on the right.
Style: perfectly flat clean graphic with sharp straight edges and solid uniform colors, front view.
Composition: one centered horizontal flag occupying 90 percent of the canvas width with small transparent margins, preserve the stated flag proportions. Square canvas.
Constraints: transparent background outside the flag; all white parts inside the flag must be opaque white. No text, no letters, no flagpole, no border, no waving, no folds, no texture, no shadow, no bevel, no rounded corners, no other objects.
```

