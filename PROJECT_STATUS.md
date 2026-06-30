# PROJECT STATUS

## Aktualna wersja

- Wersja prototypu: `0.2.0-mvp-test`
- Revit: 2025 / 2025.03
- Status: prototyp funkcjonalny, nie produkcyjny
- Obowiazek procesu: po kazdym zakonczonym zadaniu Codex aktualizuje ten plik.

## Ukonczone funkcje

- Zakladka ribbonu `PZT`.
- Przyciski: `Przygotuj PZT`, `Przypisz typ`, `MPZP`, `Bilans obszarow`.
- Parametry PZT dla obszarow i regionow wypelnienia.
- Stale typy PZT zamiast dowolnego wpisywania kategorii.
- Bilans powierzchni dzialki, zabudowy, utwardzen, PBC, intensywnosci i parkingow.
- Walidacja MPZP z komunikatami sukcesu/bledu i rachunkiem.
- Style graficzne `pztGen_*` dla wypelnien i obwiedni regionow.
- Eksport CSV.
- Serwis `PztBalanceService` dla budowania raportu.
- Serwis `MpzpValidationService` dla walidacji MPZP.
- Projekt `PztGenerator.Tests` z testami kalkulacji.
- Instrukcja testera `TESTER_GUIDE.md`.
- Widoczne oznaczenie w oknie bilansu, ze wersja jest prototypem testowym.

## Znane ograniczenia

- Brak automatycznego odswiezania raportu w czasie rzeczywistym po zmianie modelu.
- Brak komercyjnego instalatora i podpisu kodu.
- Brak eksportu XLSX/PDF.
- Brak analizy chlonnosci i wariantowania urbanistycznego.
- Testy sa lekkim runnerem konsolowym, bez pelnego frameworka testowego.
- Czesc przeplywu zalezy od poprawnego przypisania typow PZT przez uzytkownika.
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
- MVP-HANDOFF: gotowe do pokazania testerom jako `0.2.0-mvp-test`; dodano instrukcje testera i widoczna informacje o prototypie.
