# PROJECT STATUS

## Aktualna wersja

- Wersja prototypu: `0.2.7-mvp-test`
- Revit: 2025 / 2025.03
- Revit LT: nieobslugiwany, brak obslugi dodatkow Revit API
- Status: prototyp funkcjonalny, nie produkcyjny
- Obowiazek procesu: po kazdym zakonczonym zadaniu Codex aktualizuje ten plik.

## Ukonczone funkcje

- Zakladka ribbonu `PZT`.
- Przyciski: `Przygotuj PZT`, `Przypisz typ`, `MPZP`, `Bilans obszarow`.
- Parametry PZT dla obszarow i regionow wypelnienia.
- Stale typy PZT zamiast dowolnego wpisywania kategorii.
- Bilans powierzchni dzialki, zabudowy, utwardzen, automatycznej PBC, intensywnosci i parkingow.`r`n- Bilanse czastkowe dla wielu dzialek wedlug parametru tekstowego `PZT_Dzialka`.
- Bilans rozdziela pozycje wedlug kategorii i stanu, np. projektowana/istniejaca.
- PBC liczona automatycznie z granicy dzialki: powierzchnia dzialki minus zabudowa i utwardzenia plus biologiczna czesc nawierzchni przepuszczalnych.
- Walidacja MPZP z komunikatami sukcesu/bledu i rachunkiem.
- Walidacja MPZP przeniesiona do zakladki `MPZP`, razem z wymaganiami planu.
- Style graficzne `pztGen_*` dla wypelnien i obwiedni regionow.
- Eksport CSV.
- Eksport DOCX bilansu jako tabelaryczny raport.
- Eksport DOCX walidacji MPZP z lista warunkow i statusem spelnienia.
- Zwarty uklad DOCX przygotowany pod wklejenie tabel do opisu PAB/PT.
- Uproszczona zakladka `Typy`: tymczasowo tylko typ, kategoria i stan.
- Prosty instalator testerski ZIP: `dist/PztGenerator-0.2.7-mvp-test-installer.zip`.
- Instrukcja instalacji i minimalnego testu w paczce instalacyjnej.
- Serwis `PztBalanceService` dla budowania raportu.
- Serwis `MpzpValidationService` dla walidacji MPZP.
- Projekt `PztGenerator.Tests` z testami kalkulacji.
- Instrukcja testera `TESTER_GUIDE.md`.
- Widoczne oznaczenie w oknie bilansu, ze wersja jest prototypem testowym.

## Znane ograniczenia

- Brak automatycznego odswiezania raportu w czasie rzeczywistym po zmianie modelu.
- Brak komercyjnego instalatora i podpisu kodu.
- Instalator testerski jest prostym skryptem PowerShell/BAT, bez UI instalatora MSI/EXE.
- Instalator testerski blokuje Revit LT i wymaga pelnej wersji Revit 2025.
- Brak eksportu XLSX/PDF.
- Eksport DOCX jest prostym raportem testowym MVP, bez szablonu firmowego.
- Brak analizy chlonnosci i wariantowania urbanistycznego.
- Testy sa lekkim runnerem konsolowym, bez pelnego frameworka testowego.
- Czesc przeplywu zalezy od poprawnego przypisania typow PZT przez uzytkownika.
- Automatyczna PBC wymaga przypisania granicy dzialki; recznie rysowana PBC pozostaje tylko awaryjna, gdy nie ma granicy.`r`n- Bilanse wielu dzialek w MVP wymagaja recznego wpisania tego samego `PZT_Dzialka` na granicy i elementach; brak jeszcze automatycznego przypisania po geometrii.
- MVP wymaga testow na kopii modelu albo prostym modelu testowym.

## Nastepne zadania

- Uporzadkowac parametryzacje typow i zapis ustawien globalnych projektu.
- Doprecyzowac model parkingow i wymagan parkingowych.
- Poprawic UX zakladek `Typy` i `Grafika`.
- Przygotowac strategie instalacji i podpisu dopiero po stabilizacji modelu danych.
- Rozszerzyc testy serwisow bez zaleznosci od UI.

## Status sprintow

- GT-02: zakonczony jako prototyp funkcjonalny `v0.2`.
- GT-03: zakonczony w kodzie; zakres obejmuje ribbon MPZP, serwisy bilansu i walidacji, testy oraz dokumentacje.
- GT-004: gotowe jako zasada procesu; kazde kolejne zadanie musi konczyc sie aktualizacja `PROJECT_STATUS.md`.
- MVP-HANDOFF: gotowe do pokazania testerom jako `0.2.7-mvp-test`; dodano instrukcje testera, widoczna informacje o prototypie i eksport DOCX.
- GT-005: zakonczony w kodzie; dodano eksport DOCX i scalono walidacje z zakladka `MPZP`.
- GT-006: zakonczony w kodzie; dodano prosty instalator testerski, deinstalator, ZIP i instrukcje testu.
- GT-007: zakonczony w kodzie; uproszczono typy, zmieniono etykiete `Status` na `Stan` i poprawiono format DOCX.
- GT-008: zakonczony w kodzie; doprecyzowano brak wsparcia Revit LT i dodano blokade LT w instalatorze.
- GT-009: zakonczony w kodzie; PBC liczy sie automatycznie z granicy dzialki, a stare wartosci typu `Zabudowa istniejaca` sa mapowane do kategorii `Zabudowa` i stanu `Istniejaca`.`r`n- GT-010: zakonczony w kodzie; regiony wypelnienia czytaja natywna powierzchnie Revita z parametru `HOST_AREA_COMPUTED`, zamiast przeliczac ja z geometrii widoku.`r`n- GT-011: zakonczony w kodzie; dodano parametr `PZT_Dzialka`, przypisywanie indeksu dzialki i zakladke `Dzialki` z bilansami czastkowymi oraz bilans calosciowy.

## Standalone - aktualny prototyp

- Standalone rozwijany oddzielnie w `standalone/` jako prosty kalkulator bilansu PZT do codziennej pracy.
- Glowna tabela danych zostala uproszczona: kategoria, stan, powierzchnia i uwagi; powierzchnia calkowita budynkow jest jednym polem MPZP, a nie kolumna w kazdym wierszu.
- Import/eksport przeniesiony do menu w lewym gornym rogu; DXF pozostaje modulem alfa/eksperymentalnym.
- Usunieto duplikujaca zakladke `Bilans`; glowne okno jest miejscem wprowadzania danych, a panel MPZP pokazuje wymagania i walidacje.
- Preview opublikowany w `standalone/dist/PztGenerator-Standalone-preview-simplified-input/`.

## Standalone - pliki projektu

- Dodano zapis i odczyt projektu standalone jako `.pzt.json`: dane tabeli, wymagania MPZP, tryb PBC, PBC reczna i powierzchnia calkowita budynkow.
- Menu `Plik` zawiera teraz `Nowy`, `Otworz`, `Zapisz`, `Zapisz jako` oraz eksporty.
- Naglowek aplikacji pokazuje aktualnie otwarty plik projektu albo stan `Projekt: niezapisany`.
- Preview opublikowany w `standalone/dist/PztGenerator-Standalone-preview-project-files/`.

## M3 - eksport DOCX do opisu PAB

- Aktualny etap: M3, dopracowanie eksportu DOCX jako priorytet przed kolejnymi funkcjami.
- Eksport bilansu DOCX generuje teraz uporzadkowany dokument do czesci opisowej projektu architektoniczno-budowlanego.
- Dodano trzy bloki tabel: podstawowe wskazniki, bilans powierzchni terenu oraz sprawdzenie wymagan MPZP.
- Tabele maja stale szerokosci kolumn, naglowki, subtelne tlo oraz kolorowe statusy walidacji: spelniony, niespelniony, uwaga.
- Dodano test, ktory generuje DOCX, sprawdza strukture ZIP/DOCX, poprawny XML dokumentu oraz obecnosc stylowanych tabel.
- Preview opublikowany w `standalone/dist/PztGenerator-Standalone-preview-docx-pab/`.

## M3 - DOCX: rozbicie wg stanu

- Bilans powierzchni w DOCX rozdziela elementy wedlug stanu: projektowane, istniejace oraz ewentualnie bez okreslonego stanu.
- Kazda grupa ma naglowek, wiersze szczegolowe oraz sume czesciowa.
- Na koncu tabeli dodano laczna sume projektowanych i istniejacych elementow.
- Test DOCX sprawdza teraz obecnosc grup, sum czesciowych i sumy calkowitej w wygenerowanym dokumencie.
- Preview opublikowany w `standalone/dist/PztGenerator-Standalone-preview-docx-states/`.


- GT-012: zakonczony w kodzie; poprawiono zapis indeksu dzialki do zapasowego magazynu elementu, zeby zakladka Dzialki wypelniala sie rowniez wtedy, gdy parametr projektu PZT_Dzialka nie byl dostepny na regionie wypelnienia.
- GT-013: zakonczony w kodzie; rozbito bilans dzialek na zabudowe projektowana/istniejaca oraz kategorie komunikacji: dojazdy utwardzone, powierzchnia gruntowa, dojscia, place, schody terenowe i parking. Powierzchnia gruntowa nie pomniejsza PBC.
