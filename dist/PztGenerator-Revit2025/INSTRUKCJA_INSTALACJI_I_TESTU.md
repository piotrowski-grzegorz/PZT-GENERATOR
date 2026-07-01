# PZT Generator MVP - instrukcja instalacji i testu

Wersja: `0.2.2-mvp-test`

To jest prototyp testowy dla pelnej wersji Autodesk Revit 2025 / 2025.03. Nie jest to jeszcze wersja produkcyjna.

Uwaga: Revit LT nie obsluguje klasycznych dodatkow Revit API, wiec PZT Generator nie zadziala pod Revit LT.

## 1. Co jest w paczce

- `PztGenerator.dll` - plik wtyczki.
- `INSTALUJ_PZT_GENERATOR.bat` - najprostsza instalacja przez dwuklik.
- `Install-PZT-Generator.ps1` - instalator PowerShell.
- `ODINSTALUJ_PZT_GENERATOR.bat` - odinstalowanie przez dwuklik.
- `Uninstall-PZT-Generator.ps1` - deinstalator PowerShell.
- `INSTRUKCJA_INSTALACJI_I_TESTU.md` - ten plik.

## 2. Instalacja

1. Zamknij Revit.
2. Rozpakuj paczke do dowolnego folderu, np. `C:\Temp\PZT_Generator`.
3. Kliknij dwa razy `INSTALUJ_PZT_GENERATOR.bat`.
4. Instalator zapyta o sciezke folderu Revit 2025.
   Przyklady:
   - `C:\Program Files\Autodesk\Revit 2025`
   - `E:\Program Files\Autodesk\Revit 2025`
5. Nie wskazuj folderu Revit LT. Instalator przerwie instalacje dla Revit LT.
6. Po komunikacie o udanej instalacji uruchom Revit 2025.
7. W Revicie powinna pojawic sie zakladka `PZT`.

Instalator kopiuje wtyczke do:

```text
%AppData%\GRAFEL\PZT Generator\Revit2025
```

oraz tworzy manifest Revita:

```text
%AppData%\Autodesk\Revit\Addins\2025\PztGenerator.addin
```

## 3. Odinstalowanie

1. Zamknij Revit.
2. Kliknij dwa razy `ODINSTALUJ_PZT_GENERATOR.bat`.
3. Uruchom ponownie Revit.

## 4. Minimalny przyklad dzialania

1. Otworz prosty plik testowy w Revit 2025.
2. Na rzucie PZT narysuj `Region wypelnienia` jako granice dzialki.
3. Zaznacz region dzialki.
4. Kliknij `PZT > Przygotuj PZT`, jezeli projekt nie ma jeszcze parametrow PZT.
5. Kliknij `PZT > Przypisz typ` i wybierz `Granica terenu / dzialki`.
6. Narysuj region budynku.
7. Zaznacz region budynku, kliknij `PZT > Przypisz typ` i wybierz `Zabudowa projektowana`.
8. Narysuj dojazd albo dojscie i przypisz mu typ `Dojazdy` albo `Dojscia`.
9. Narysuj parking i przypisz typ `Parking`.
10. Kliknij `PZT > MPZP` i wpisz przykladowe wymagania:
    - min. PBC: `30`
    - max. powierzchnia zabudowy: `40`
    - min. intensywnosc: `0.2`
    - max. intensywnosc: `1.0`
11. Kliknij `PZT > Bilans obszarow`.
12. W zakladce `Bilans` sprawdz powierzchnie i wskazniki.
13. W zakladce `MPZP` sprawdz warunki oraz wynik `spelniony` / `niespelniony`.
14. Kliknij `Eksport DOCX`, aby zapisac caly bilans.
15. W zakladce `MPZP` kliknij `Eksport MPZP DOCX`, aby zapisac tylko liste warunkow.
16. W zakladce `Grafika` kliknij `Zastosuj style do regionow`, jezeli chcesz odswiezyc kolory, wypelnienia i obwiednie.

## 5. Co sprawdzamy w MVP

- Czy zakladka `PZT` pojawia sie po instalacji.
- Czy przypisywanie typow jest zrozumiale.
- Czy dzialka, zabudowa, dojazdy, dojscia i parking zliczaja sie poprawnie.
- Czy walidacja MPZP pokazuje czytelny rachunek.
- Czy eksport DOCX jest wystarczajaco czytelny do wewnetrznego testu.
- Czy grafika regionow pomaga w pracy na rysunku.

## 6. Czego ta wersja jeszcze nie ma

- automatycznego odswiezania raportu w czasie rzeczywistym,
- firmowego szablonu DOCX,
- eksportu XLSX,
- eksportu PDF,
- instalatora komercyjnego,
- podpisu kodu,
- licencjonowania,
- analizy chlonnosci.

## 7. Zglaszanie uwag

Dla kazdej uwagi zapisz:

- co probowales zrobic,
- co sie stalo,
- czego oczekiwales,
- screen, jezeli problem dotyczy UI albo grafiki,
- przyblizone wartosci powierzchni, jezeli problem dotyczy bilansu.
