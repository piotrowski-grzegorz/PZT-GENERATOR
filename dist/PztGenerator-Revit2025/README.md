# PZT Generator - Revit 2025

Uwaga: paczka wymaga pelnej wersji Autodesk Revit 2025. Revit LT nie obsluguje klasycznych dodatkow Revit API, wiec PZT Generator nie zadziala pod Revit LT.

## Aktualna wersja prototypu

- zakladka `PZT` w Revicie
- przycisk `Przygotuj PZT`
- przycisk `Przypisz typ`
- przycisk `MPZP`
- przycisk `Bilans obszarow`
- automatyczne dodawanie parametrow:
  - `PZT_Kategoria`
  - `PZT_Wspolczynnik_Bio`
  - `PZT_Status`
  - `PZT_Liczba_Kondygnacji`
  - `PZT_Wysokosc_Kondygnacji`
  - `PZT_Uwagi`
  - wymagania MPZP w informacjach o projekcie
- bilans obszarow w oknie tabelarycznym
- bilans liczy tez regiony wypelnienia, ktore moga nachodzic na siebie bez pomniejszania dzialki
- `Przypisz typ` dziala na powierzchniach i regionach wypelnienia
- regiony wypelnienia dostaja automatyczny typ graficzny PZT z kolorem i gruboscia linii
- wskazniki urbanistyczne i walidacja MPZP
- MPZP pokazuje przeliczenie procentow na m2 na podstawie obszaru `Granica terenu / dzialki`
- raport pokazuje powierzchnie utwardzona z dojazdow, dojsc i parkingow
- bledne reczne wartosci `PZT_Kategoria` sa oznaczane jako `Nieprzypisane / bledne`
- przyciski ribbonu maja ikony
- eksport bilansu do CSV
- eksport bilansu do DOCX
- eksport walidacji MPZP do DOCX

## Instalacja z paczki

Najprostsza metoda dla testera:

1. Rozpakuj paczke ZIP do dowolnego folderu.
2. Zamknij Revit.
3. Kliknij dwa razy `INSTALUJ_PZT_GENERATOR.bat`.
4. Podaj sciezke folderu pelnego Revit 2025, np. `E:\Program Files\Autodesk\Revit 2025`.
5. Uruchom ponownie Revit 2025.

Alternatywnie z PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-PZT-Generator.ps1
```

Odinstalowanie:

```powershell
powershell -ExecutionPolicy Bypass -File .\Uninstall-PZT-Generator.ps1
```
