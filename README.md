# PZT Generator

Prototyp wtyczki do pelnej wersji Autodesk Revit 2025.03 wspierajacej bilans PZT: granica dzialki, zabudowa, dojazdy, dojscia, parkingi, PBC oraz podstawowa walidacja MPZP.

Uwaga: Revit LT nie obsluguje klasycznych dodatkow Revit API, wiec PZT Generator nie zadziala pod Revit LT.

GT-02 jest prototypem funkcjonalnym, a GT-03 stabilizuje kod i dokumentacje dla wersji `0.2.0`. Wersja `0.2.2-mvp-test` jest przeznaczona do testow wewnetrznych. To nie jest jeszcze wersja produkcyjna ani komercyjny instalator.

Instrukcja dla testerow: `TESTER_GUIDE.md`.

Paczka instalacyjna dla testerow:

```text
dist/PztGenerator-0.2.2-mvp-test-installer.zip
```

## Aktualny ribbon

Zakladka `PZT`, panel `Bilans`:

- `Przygotuj PZT` - dodaje parametry projektu PZT.
- `Przypisz typ` - przypisuje zaznaczonym obszarom/regionom staly typ PZT i domyslne parametry.
- `MPZP` - ustawia wymagania MPZP uzywane do walidacji bilansu PZT.
- `Bilans obszarow` - otwiera raport, walidacje, ustawienia MPZP, parking, typy i grafike.

## Zakres prototypu

- pelny Revit 2025 / .NET 8
- Revit LT: nieobslugiwany
- odczyt natywnych `Areas` oraz `FilledRegion`
- slownik stalych typow PZT zamiast dowolnego wpisywania kategorii
- bilans powierzchni wedlug kategorii i stanu
- bilans rozdzielony wedlug kategorii i stanu, np. projektowana/istniejaca
- powierzchnia dzialki z typu `Granica terenu / dzialki`
- powierzchnia zabudowy i wskaznik zabudowy
- powierzchnia biologicznie czynna i wskaznik PBC
- powierzchnia calkowita i intensywnosc zabudowy
- miejsca parkingowe liczone z powierzchni parkingu i ustawien miejsc
- walidacja min/max MPZP z opisem rachunku
- wymagania i walidacja MPZP w jednej zakladce `MPZP`
- style wypelnien i obwiedni regionow `pztGen_*`
- eksport raportu do CSV i DOCX
- eksport listy warunkow MPZP do DOCX
- zwarty format DOCX z tabelami do dalszego wykorzystania w opisie PAB/PT
- uproszczona zakladka `Typy`: tylko typ, kategoria i stan

## Workflow testowy

1. Uruchom Revit 2025.
2. Otworz lub przygotuj widok PZT.
3. Narysuj regiony wypelnienia dla:
   - granicy terenu,
   - zabudowy,
   - dojazdow/dojsc,
   - parkingu,
   - obszarow PBC lub czesciowo biologicznie czynnych.
4. Kliknij `PZT > Przygotuj PZT`.
5. Zaznacz region i kliknij `PZT > Przypisz typ`.
6. Dla granicy wybierz `Granica terenu / dzialki`.
7. Dla budynkow wybierz `Zabudowa projektowana` albo `Zabudowa istniejaca`.
8. Kliknij `PZT > MPZP` i wpisz wymagania planu.
9. Kliknij `PZT > Bilans obszarow`.
10. W zakladce `Typy` mozesz zmienic kondygnacje, wysokosc kondygnacji i wspolczynnik PBC dla zaznaczonych regionow.
11. W zakladce `MPZP` sprawdz liste warunkow i uzyj `Eksport MPZP DOCX`, jezeli chcesz zapisac sama walidacje.
12. Uzyj `Eksport DOCX`, jezeli chcesz zapisac caly bilans jako raport tabelaryczny.
13. W zakladce `Grafika` uzyj `Zastosuj style do regionow`, jezeli trzeba odswiezyc wypelnienia i obwiednie.

Nie wpisuj recznie dowolnych wartosci w `PZT_Kategoria`. Raport rozpoznaje tylko stale typy PZT nadawane przez wtyczke.

## Budowanie

Domyslna sciezka Revita to:

```powershell
C:\Program Files\Autodesk\Revit 2025
```

Jesli Revit jest w innej lokalizacji:

```powershell
dotnet build .\src\PztGenerator\PztGenerator.csproj -c Release /p:RevitInstallDir="E:\Program Files\Autodesk\Revit 2025"
```

Mozesz tez skopiowac `Directory.Build.props.example` do `Directory.Build.props` i wpisac lokalna sciezke. Ten plik jest ignorowany przez Git.

## Testy

Projekt `tests\PztGenerator.Tests` jest lekkim runnerem konsolowym bez zewnetrznego frameworka testowego.

```powershell
dotnet run --project .\tests\PztGenerator.Tests\PztGenerator.Tests.csproj -c Release /p:RevitInstallDir="E:\Program Files\Autodesk\Revit 2025"
```

Testy obejmuja:

- PBC,
- wskaznik zabudowy,
- intensywnosc,
- walidacje min/max MPZP.

## Instalacja dla testera

Najprostsza instalacja dla kolegi/testera:

1. Przekaz paczke `dist/PztGenerator-0.2.2-mvp-test-installer.zip`.
2. Tester rozpakowuje ZIP do dowolnego folderu.
3. Tester klika `INSTALUJ_PZT_GENERATOR.bat`.
4. Instalator pyta o sciezke folderu Revit 2025, np. `E:\Program Files\Autodesk\Revit 2025`.
5. Po restarcie Revita powinna pojawic sie zakladka `PZT`.

Nie wskazuj folderu Revit LT. Ta wersja Revita nie laduje dodatkow Revit API.

Instalator kopiuje DLL do:

```powershell
%AppData%\GRAFEL\PZT Generator\Revit2025
```

i tworzy manifest:

```powershell
%AppData%\Autodesk\Revit\Addins\2025\PztGenerator.addin
```

To jest prosty instalator testerski PowerShell/BAT, nie komercyjny instalator MSI/EXE.

## Instalacja lokalna deweloperska

Po zbudowaniu:

```powershell
.\tools\Install-Addin.ps1
```

Skrypt tworzy plik `.addin` w:

```powershell
%AppData%\Autodesk\Revit\Addins\2025
```

Po restarcie Revita powinna pojawic sie zakladka `PZT`.

## Poza zakresem GT-03 / MVP

- XLSX,
- PDF,
- AI,
- analiza chlonnosci,
- licencjonowanie,
- komercyjny instalator,
- automatyczne odswiezanie realtime.
