# ElfIntegrator
Izvrsava integrale koji su definisani u ulaznom `.emt` fajlu. Svaki integral mora da sadrzi funkciju (izraz koji ne koristi funkcije kao sin/cos/faktorijel, vrlo primitivno), gornju i donju granicu, korak i broj niti na kojima ce se taj integral izvrsavati.
Rezultati integrala se po zavrsetku upisuju u fajl u LocalStorage pod nazvom Results.txt.

Svi integrali se cuvaju i nakon zatvaranja aplikacije. Nedovrseni integrali se mogu nastaviti dalje od istog mjesta gdje su i stali.

Istorija se moze obrisati pomocu UI elementa koji brise sve taskove iz istorije.

Postoji asociacija sa .emt fajlovima koji moraju imati ispravnu XML strukturu kao npr
```xml
<ElfMathTasks>
    <ElfMathTask title="Neki kul integral">
        <Expression>x^3-3*x^2-8*x+6</Expression>
        <UpperBound>10</UpperBound>
        <LowerBound>0</LowerBound>
        <Step>0.000001</Step>
        <ElfCount>3</ElfCount>
    </ElfMathTask>
</ElfMathTasks>
```
Gdje su polja:
* `Expression` - Funkcija ciji integral trazimo
  * `title` - naslov tog integrala u korisnickom interfejsu
* `UpperBound` - Gornja granica integrala
* `LowerBound` - Donja granica integrala
* `Step` - Velicina koraka integracija
* `ElfCount` - Broj threadova na kojima se izvrsava odredjeni integral


______
_____
____
