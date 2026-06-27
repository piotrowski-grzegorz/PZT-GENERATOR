# PZT Generator

Pierwszy prototyp wtyczki do Revit 2025.03. Na tym etapie dodatek czyta natywne obszary Revita (`Areas`) i pokazuje prosty bilans powierzchni pogrupowany po parametrze `PZT_Kategoria`.

## Zakres MVP

- Revit 2025 / .NET 8
- zakladka `PZT` w ribbonie
- przycisk `Przygotuj PZT`, ktory dodaje parametry do obszarow
- przycisk `Przypisz typ`, ktory ustawia gotowe kategorie PZT na zaznaczonych obszarach
- przycisk `MPZP`, ktory zapisuje wymagania planu miejscowego w informacjach o projekcie
- przycisk `Bilans obszarow`
- odczyt obszarow z modelu
- suma powierzchni w m2 wedlug parametru `PZT_Kategoria`
- wyliczenie powierzchni biologicznie czynnej z parametru `PZT_Wspolczynnik_Bio`
- czytelne okno tabelaryczne bilansu
- eksport bilansu do pliku CSV
- raport wskaznikow urbanistycznych:
  - powierzchnia zabudowy
  - wskaznik powierzchni zabudowy
  - powierzchnia biologicznie czynna
  - wskaznik PBC
  - powierzchnia calkowita
  - intensywnosc zabudowy
- walidacja wzgledem wymagan MPZP

## Jak przygotowac model testowy

1. W Revicie utworz `Area Plan`.
2. Narysuj kilka `Areas`.
3. Kliknij `PZT > Przygotuj PZT`, zeby dodac parametry.
4. Zaznacz obszar lub kilka obszarow i kliknij `PZT > Przypisz typ`.
5. Wybierz np.:
   - `Granica terenu / dzialki`
   - `Zabudowa projektowana`
   - `Zabudowa istniejaca`
   - `Dojazdy`
   - `Parking`
   - `Biologicznie czynna`
6. Kliknij `PZT > MPZP` i wpisz wymagania planu miejscowego.
7. Uruchom przycisk `PZT > Bilans obszarow`.

Powierzchnia dzialki jest liczona z obszaru przypisanego jako `Granica terenu / dzialki`. W MPZP procenty sa od razu przeliczane na m2 na podstawie tej powierzchni.

Nie wpisuj recznie dowolnych wartosci w `PZT_Kategoria`. Do przypisywania uzywaj przycisku `Przypisz typ`, bo raport rozpoznaje tylko stale typy PZT. Jezeli parametr jest pusty, obszar trafi do grupy `Bez kategorii`. Jezeli wpisano wartosc spoza slownika, trafi do `Nieprzypisane / bledne`.

## Konfiguracja sciezki do Revita

Projekt zaklada standardowa lokalizacje Revita:

`C:\Program Files\Autodesk\Revit 2025`

Jezeli Revit jest zainstalowany gdzie indziej, znajdz `RevitAPI.dll`:

```powershell
.\tools\Find-RevitApi.ps1
```

Potem zbuduj projekt z parametrem:

```powershell
dotnet build .\src\PztGenerator\PztGenerator.csproj -c Debug /p:RevitInstallDir="C:\Twoja\Sciezka\Do\Revit 2025"
```

Mozesz tez skopiowac `Directory.Build.props.example` do `Directory.Build.props` i wpisac tam wlasciwa sciezke. Ten plik jest ignorowany przez git, bo zalezy od konkretnego komputera.

## Budowanie

Budowanie:

```powershell
dotnet build .\src\PztGenerator\PztGenerator.csproj -c Debug
```

## Instalacja lokalna

Po zbudowaniu uruchom:

```powershell
.\tools\Install-Addin.ps1
```

Skrypt utworzy plik `.addin` w:

`%AppData%\Autodesk\Revit\Addins\2025`

Po restarcie Revita powinna pojawic sie zakladka `PZT`.

## Kolejne kroki

Najpierw warto dopracowac slownik kategorii i parametry. Dopiero potem dodac:

- okno tabelaryczne zamiast prostego komunikatu,
- narzedzie do automatycznego dodawania parametru `PZT_Kategoria`,
- eksport do Excela/CSV,
- automatyczne odswiezanie bilansu po zmianach modelu,
- instalator dla innych komputerow.
