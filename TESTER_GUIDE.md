# TESTER GUIDE - PZT Generator MVP

## Cel testu

Sprawdzamy workflow i logike bilansu PZT w prototypie `0.2.0-mvp-test` dla Revit 2025.03. To nie jest narzedzie do finalnej dokumentacji ani rozliczen formalnych.

## Przed testem

- Uruchom Revit ponownie po instalacji wtyczki.
- Sprawdz, czy widoczna jest zakladka `PZT`.
- Testuj na kopii modelu albo na prostym pliku testowym.

## Minimalny scenariusz demo

1. Narysuj region wypelnienia jako dzialke, np. okolo `1000 m2`.
2. Zaznacz region dzialki i wybierz `PZT > Przypisz typ > Granica terenu / dzialki`.
3. Narysuj region budynku, np. `200 m2`.
4. Zaznacz budynek i wybierz `Zabudowa projektowana`.
5. Otworz `PZT > Bilans obszarow > Typy`, wybierz `Zabudowa projektowana`, ustaw np. `3` kondygnacje i zastosuj do zaznaczonego budynku.
6. Narysuj dojazd/dojscie, np. `100 m2`, i przypisz `Dojazdy` albo `Dojscia`.
7. Narysuj parking i przypisz `Parking`.
8. Kliknij `PZT > MPZP` i wpisz przykladowo:
   - min. PBC: `30%`,
   - max. powierzchnia zabudowy: `40%`,
   - min. intensywnosc: `0.2`,
   - max. intensywnosc: `1.0`.
9. Kliknij `PZT > Bilans obszarow`.
10. Sprawdz zakladki `Bilans`, `Walidacja MPZP`, `MPZP`, `Typy`, `Grafika`.
11. W zakladce `Grafika` kliknij `Zastosuj style do regionow` i sprawdz obwiednie oraz wypelnienia.

## Co tester ma ocenic

- Czy workflow przypisywania typow jest zrozumialy.
- Czy nazwy kategorii PZT sa jasne.
- Czy bilans pokazuje oczekiwane wartosci.
- Czy walidacja MPZP czytelnie tlumaczy rachunek.
- Czy grafika regionow pomaga w pracy na rysunku.
- Czy komunikaty bledow wystarczaja, gdy brakuje granicy dzialki albo typu PZT.

## Czego nie testujemy w MVP

- Eksportu XLSX/PDF.
- Automatycznego odswiezania realtime.
- Licencjonowania.
- Instalatora komercyjnego.
- Analizy chlonnosci.
- AI.

## Jak zglaszac uwagi

Dla kazdej uwagi zapisz:

- co probowales zrobic,
- co sie stalo,
- czego oczekiwales,
- screen, jesli problem dotyczy UI albo grafiki,
- przyblizone wartosci powierzchni, jesli problem dotyczy bilansu.
