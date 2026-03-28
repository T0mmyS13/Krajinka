# Zpracované zadání semestrální práce (Krajinka)

## 1) Cíl práce
Implementovat jednoduchý 3D renderer krajiny s interaktivním pohybem pozorovatele.

Důraz:
- geometrie
- transformace
- čas
- vstup
- shader pipeline

---

## 2) Povinné technické požadavky
- Jazyk: `C#`
- Runtime: `.NET 8+`
- Grafika: `OpenGL 3.3 Core Profile`
- Knihovna: `OpenTK`
- Povinně použít:
  - `vertex shader` + `fragment shader`
  - `VBO` + `VAO`
  - vlastní transformační matice: `model / view / projection`

---

## 3) Vstupní data terénu (RGBA PNG)
- `R` (red): výška terénu (`0–255`)
- `G` (green): objekty
  - `0` = nic
  - `1` = strom
  - `2` = kámen
- `B` (blue): volné použití (rozšíření)
- `A` (alpha): volné použití (rozšíření)

Výška:
- mapování lineárně
- `1 jednotka = 5 cm`
- terén na pravidelné mřížce

---

## 4) Geometrie a měřítko
- Jednotky scény: `1 jednotka = 1 metr`

Terén:
- minimálně `512 x 512` vzorků
- rozestup vzorků: `0.5 m`
- výsledná plocha cca `256 x 256 m`

Pozorovatel:
- výška očí: `1.8 m`
- rychlost chůze: jako Doom (1993), bez Shift
- pohyb nezávislý na FPS
- chodí po povrchu terénu
- nesmí pod terén
- nesmí mimo mapu

---

## 5) Ovládání
Klávesnice:
- `W` vpřed
- `S` vzad
- `A` vlevo
- `D` vpravo
- směr podle orientace kamery
- diagonála nesmí zvýšit rychlost

Myš:
- yaw: vlevo/vpravo
- pitch: nahoru/dolů
- pitch omezen na `<-90°, +90°>`

---

## 6) Čas a FPS
- Všechny pohyby/animace řídit reálným časem (`delta time`)
- FPS nesmí ovlivnit rychlost pohybu ani animací
- Zobrazit FPS čítač jako klouzavý interval 1 sekundy

---

## 7) Osvětlení (povinné minimum)
- Jednoduché globální světlo (slunce)
- Výpočet osvětlení ve shaderu
- `Lambert` nebo jednoduchý `Phong`

---

## 8) Povinná část – akceptační checklist
Práce musí splnit:
- [ ] vykreslení terénu z výškové mapy
- [ ] pohyb pozorovatele po terénu (včetně interpolace výšky)
- [ ] korektní kolize s terénem
- [ ] omezení pohybu na hranice mapy
- [ ] shaderový rendering
- [ ] pohyb/animace závislé na reálném čase
- [ ] FPS čítač (klouzavý)
- [ ] stabilní chod aplikace

---

## 9) Rozšiřující část (volitelné)
Možné oblasti:
- geometrie/výkon (`wireframe`, `frustum culling`, `LOD`, hladký terén)
- terén/objekty (stromy, kameny, sklon, šum, animace)
- vizuální efekty (textury, mlha, voda)
- atmosféra/osvětlení (den/noc, světla, stíny)
- interakce/hra (sběratelné objekty, teleport, minimapa, pathfinding)
- manipulace terénu (odebírání/přidávání hmoty)

---

## 10) Dokumentace a odevzdání
Dokumentace:
- formát `PDF`
- stručně a věcně
- obsah:
  - popis řešení terénu
  - popis kolizí
  - seznam implementovaných rozšíření

Doporučená věta do dokumentace:
> Souhlasím s vystavením této semestrální práce na stránkách katedry informatiky a výpočetní techniky a jejímu využití pro prezentaci pracoviště.

Struktura archivu:
- `src/` zdrojové kódy + .NET projekt
- `bin/` spustitelná aplikace + data
- `doc/` dokumentace PDF

Projekt musí jít spustit bez úprav (`dotnet run`).

---

## 11) Poznámky k hodnocení
- První část: jedno odevzdání, pozdní odevzdání penalizace `20 % / den`, po 5 dnech `0 bodů`
- Druhá část: opravy + rozšíření, zpoždění se sčítá v rámci druhé části
- Za každé vrácení ve druhé části: `-10 bodů`
- Maximální celkový zisk: `40 bodů`

---

## 12) Praktický cíl pro aktuální projekt
Krátkodobý cíl implementace:
1. Načítat terén z RGBA mapy
2. Převést `R` na výšku mesh/gridu
3. Přidat pohyb kamery po povrchu (výška + kolize + hranice)
4. Udržet `delta time` pohyb
5. Udržet shader pipeline (`VAO/VBO`, `model/view/projection`)
6. Dodat základní osvětlení ve shaderu (`Lambert`)
