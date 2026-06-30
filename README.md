# PZT Generator

Prototyp wtyczki do Autodesk Revit 2025.03 wspierajacej bilans PZT: granica dzialki, zabudowa, dojazdy, dojscia, parkingi, PBC oraz podstawowa walidacja MPZP.

GT-02 jest prototypem funkcjonalnym, a GT-03 stabilizuje kod i dokumentacje dla wersji `0.2.0`. Wersja `0.2.0-mvp-test` jest przeznaczona do testow wewnetrznych. To nie jest jeszcze wersja produkcyjna ani komercyjny instalator.

Instrukcja dla testerow: `TESTER_GUIDE.md`.

## Aktualny ribbon

Zakladka `PZT`, panel `Bilans`:

- `Przygotuj PZT` - dodaje parametry projektu PZT.
- `Przypisz typ` - przypisuje zaznaczonym obszarom/regionom staly typ PZT i domyslne parametry.
- `MPZP` - ustawia wymagania MPZP uzywane do walidacji bilansu PZT.
- `Bilans obszarow` - otwiera raport, walidacje, ustawienia MPZP, parking, typy i grafike.

## Zakres prototypu

- Revit 2025 / .NET 8
- odczyt natywnych `Areas` oraz `FilledRegion`
- slownik stalych typow PZT zamiast dowolnego wpisywania kategorii
- bilans powierzchni wedlug kategorii i statusu
- powierzchnia dzialki z typu `Granica terenu / dzialki`
- powierzchnia zabudowy i wskaznik zabudowy
- powierzchnia biologicznie czynna i wskaznik PBC
- powierzchnia calkowita i intensywnosc zabudowy
- miejsca parkingowe liczone z powierzchni parkingu i ustawien miejsc
- walidacja min/max MPZP z opisem rachunku
- style wypelnien i obwiedni regionow `pztGen_*`
- eksport raportu do CSV

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
11. W zakladce `Grafika` uzyj `Zastosuj style do regionow`, jezeli trzeba odswiezyc wypelnienia i obwiednie.

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

## Instalacja lokalna

Po zbudowaniu:

```powershell
.\tools\Install-Addin.ps1
```

Skrypt tworzy plik `.addin` w:

```powershell
%AppData%\Autodesk\Revit\Addins\2025
```

Po restarcie Revita powinna pojawic sie zakladka `PZT`.

## Poza zakresem GT-03

- XLSX,
- PDF,
- AI,
- analiza chlonnosci,
- licencjonowanie,
- komercyjny instalator,
- automatyczne odswiezanie realtime.
