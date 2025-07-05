# My Work Salary - Mobil App

## Projektbeskrivning
En mobil app för skiftarbetare att registrera arbetspass, hålla koll på arbetstid och kontrollera att lönen från arbetsgivaren stämmer.

## Målgrupp
- **Primärt**: Vårdpersonal (undersköterskor, sjuksköterskor)
- **Sekundärt**: Alla skiftarbetare som behöver kolla sin lön

## Huvudfunktioner

### Klart ✅
- [x] **Jobbhantering**: Skapa, redigera och radera jobbprofiler
- [x] **Flera jobb**: Växla mellan olika anställningar
- [x] **Passregistrering**: Registrera arbetspass, sjukdagar och semester
- [x] **Flex-timbalans**: Spårning av över-/undertid för månadslöner
- [x] **Dashboard**: Översikt av månadsstatistik och flex-saldo
- [x] **Svensk formatering**: Kronor, svenska datum och tidsformat
- [x] **Ljust/Mörkt tema**: Växla mellan teman

### Pågående utveckling 🚧
- [ ] **OB-tillägg**: Automatisk beräkning baserat på arbetstid
- [ ] **Svensk helgkalender**: Automatisk helgdagsdetektering för OB-beräkning

### Framtida funktioner 📋
- [ ] **VAB-registrering**: Vård av barn som egen kategori
- [ ] **Export**: Månadsrapporter till PDF/Excel
- [ ] **Lönejämförelse**: Jämför beräknad vs faktisk lön från arbetsgivaren

## Teknisk stack
- **Framework**: .NET MAUI
- **Databas**: SQLite
- **Arkitektur**: MVVM (Model-View-ViewModel)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Plattformar**: Android (primärt), iOS (sekundärt)

## Funktioner i detalj

### 🏠 Hem-sida (HomePage)
- Välkomstmeddelande med aktivt jobb
- Månadsstatistik (timmar, lön, arbetsdagar)
- Flex-saldo med visuell indikator
- Senaste aktiviteter
- Snabbnavigation till passregistrering

### 💼 Jobb-hantering
- **Skapa jobb**: Jobbtitel, arbetsplats, anställningstyp, löneinfo
- **Flera jobb**: Växla aktivt jobb med radiobuttons
- **Redigera**: Uppdatera jobbinformation
- **Radera**: Säker borttagning med bekräftelse
- **Anställningstyper**: Tillsvidare, Visstid, Timanställd

### ⏰ Pass-registrering (AddShiftPage)
- **Arbetstid**: Datum, start-/sluttid med tidsväljare
- **Skifttyper**: Arbetspass, Sjukdag, Semester
- **Flerdagarspass**: Registrera samma pass över flera dagar
- **Grundläggande beräkning**: Timmar och grundlön
- **Realtidsuppdatering**: Se beräknad lön medan du skriver

### 📈 Flex-timbalans
- **Månadslön**: Spårning av över-/undertid
- **Ackumulerat saldo**: Löpande balans över månader
- **Visuell feedback**: Färgkodade indikatorer
- **Historik**: Se tidigare månaders flex-utveckling

### ⚙️ Inställningar (SettingsPage)
- **Jobbväxling**: Enkelt byte mellan anställningar
- **Jobbhantering**: Lägg till, ändra, radera jobb
- **Detaljerad jobbinfo**: Lön, anställningstyp, arbetstid
- **Tema**: Växla mellan ljust och mörkt tema

## Projektstruktur
```
MyWorkSalary/
├── Models/
│   ├── JobProfile.cs           # Jobbprofil med löneinfo
│   ├── WorkShift.cs           # Arbetspass med grundberäkning
│   ├── FlexTimeBalance.cs     # Flex-timbalans per månad
│   ├── OBRate.cs              # OB-tillägg konfiguration
│   └── AppSettings.cs         # App-inställningar
├── Views/
│   ├── HomePage.xaml          # Dashboard med översikt
│   ├── SalaryPage.xaml        # Löneöversikt
│   ├── ShiftPage.xaml         # Pass-översikt
│   ├── SettingsPage.xaml      # Inställningar och jobbhantering
│   └── Pages/
│       ├── AddJobPage.xaml    # Skapa nytt jobb
│       ├── EditJobPage.xaml   # Redigera befintligt jobb
│       ├── AddOBRatePage.xaml # Konfigurera OB-tillägg
│       └── AddShiftPage.xaml  # Registrera arbetspass
├── ViewModels/
│   ├── AddJobViewModel.cs     # Skapa jobb logik
│   ├── AddOBRateViewModel.cs  # OB-konfiguration logik
│   ├── AddShiftViewModel.cs   # Pass-registrering logik
│   ├── EditJobViewModel.cs    # Redigera jobb logik
│   ├── HomeViewModel.cs       # Dashboard-logik
│   ├── SettingsViewModel.cs   # Inställningar logik
│   └── ShiftPageViewModel.cs  # Pass-översikt logik
├── Services/
│   ├── Builders/
│   │   └── ShiftBuildersServices.cs    # Pass-byggare
│   ├── Calculations/
│   │   └── ShiftCalculationService.cs  # Löneberäkningar
│   ├── Conflicts/
│   │   └── ConflictResolutionService.cs # Konfliktlösning
│   ├── Handlers/
│   │   └── ConflictHandlerService.cs    # Konflikthantering
│   ├── Interfaces/
│   │   ├── IConflictHandlerService.cs
│   │   ├── IConflictResolutionService.cs
│   │   ├── IDashboardService.cs
│   │   ├── IShiftBuilderService.cs
│   │   ├── IShiftCalculationService.cs
│   │   ├── IShiftValidationService.cs
│   │   └── IWorkShiftService.cs
│   ├── Validation/
│   │   └── ShiftValidationService.cs    # Pass-validering
│   ├── DatabaseService.cs      # SQLite databas-operationer
│   ├── WorkShiftService.cs     # Pass-hantering
│   └── DashboardService.cs     # Dashboard-data och statistik
└── Resources/
    └── Styles/
        ├── Styles.xaml         # App-stilar
        └── Colors.xaml         # Färgtema
```

## Tekniska funktioner

### 🗄️ Databas
- **SQLite**: Lokal databas med automatisk initiering
- **Relationer**: JobProfile → WorkShift → FlexTimeBalance
- **CRUD**: Fullständiga databas-operationer
- **Prestanda**: Optimerade frågor för snabb datahantering

### 🧮 Beräkningsmotor
- **Grundlön**: Beräkning baserat på timmar och timlön/månadslön
- **Flex-timmar**: Ackumulerad balans över månader
- **Månadsstatistik**: Sammanställning av arbetstid och lön
- **Validering**: Konflikthantering och datavalidering

### 🎨 UI/UX Design
- **Material Design**: Moderna kort och färgschema
- **Responsiv**: Fungerar på olika skärmstorlekar
- **Intuitive navigation**: Shell-baserad navigation
- **Realtidsuppdatering**: Data binding med automatisk UI-refresh
- **Svensk lokalisering**: Datum, tid och valuta i svenskt format
- **Tema-stöd**: Ljust och mörkt tema

### 🏗️ Arkitektur
- **MVVM**: Tydlig separation mellan UI och logik
- **Dependency Injection**: Löst kopplad och testbar kod
- **Commands**: Asynkrona användarinteraktioner
- **Services**: Modulär affärslogik med specialiserade tjänster
- **Converters**: UI-datakonvertering
- **Separation of Concerns**: Tydlig uppdelning av ansvar

## Installation och körning

### Förutsättningar
- Visual Studio 2022 (17.8+)
- .NET 8 SDK
- Android SDK (API 34+) eller iOS SDK

### Steg för steg
```bash
# 1. Klona projektet
git clone [repository-url]
cd MyWorkSalary

# 2. Återställ NuGet-paket
dotnet restore

# 3. Öppna i Visual Studio
start MyWorkSalary.sln

# 4. Välj target (Android/iOS)
# 5. Tryck F5 för att köra
```

### Första körningen
1. Appen skapar automatiskt SQLite-databas
2. Skapa din första jobbprofil
3. Registrera ditt första arbetspass
4. Se grundläggande löneberäkning och flex-saldo

## Utvecklingsmiljö
- **IDE**: Visual Studio 2022
- **SDK**: .NET 8
- **Emulator**: Android API 34+ / iOS Simulator
- **Debugging**: Hot Reload aktiverat
- **Version Control**: Git

## Syfte och användning
Appen hjälper skiftarbetare att:
- Hålla koll på sina arbetstimmar
- Beräkna förväntad lön
- Jämföra med lön från arbetsgivaren
- Spåra flex-timbalans för månadslöner
- Ha kontroll över sin arbetssituation

---
*Utvecklad för att ge skiftarbetare bättre kontroll över sin lön och arbetstid* 💪
