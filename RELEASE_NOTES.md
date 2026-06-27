# PZT Generator - Revit 2025

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
- wskazniki urbanistyczne i walidacja MPZP
- MPZP pokazuje przeliczenie procentow na m2 na podstawie obszaru `Granica terenu / dzialki`
- raport pokazuje powierzchnie utwardzona z dojazdow, dojsc i parkingow
- bledne reczne wartosci `PZT_Kategoria` sa oznaczane jako `Nieprzypisane / bledne`
- przyciski ribbonu maja ikony
- eksport bilansu do CSV

## Instalacja z paczki

1. Skopiuj folder `dist\PztGenerator-Revit2025` na komputer z Revit 2025.
2. Uruchom PowerShell w tym folderze.
3. Wykonaj:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-Release.ps1
```

4. Uruchom ponownie Revit 2025.
