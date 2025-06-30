# My Work Salary - Mobil App

## Projektbeskrivning
En mobil app för vårdpersonal att kontrollera om de får rätt OB-tillägg och hålla koll på timbalans.

## Målgrupp
- Primärt: Undersköterskor och vårdpersonal
- Sekundärt: Andra skiftarbetare

## Huvudfunktioner

### MVP (Minimum Viable Product)
- [x] Skapa jobbprofiler (fast anställd vs vikarie)
- [x] Hantera flera jobb (växla aktivt jobb)
- [x] Redigera och radera jobb
- [ ] Registrera arbetspass
- [ ] Beräkna OB-tillägg automatiskt
- [ ] Visa timbalans för fast anställda
- [ ] Månadsrapporter
- [ ] Svensk helgdagskalender

### Framtida funktioner
- [ ] Sjukskrivning och VAB
- [ ] Export till PDF/Excel
- [ ] Mörkt tema
- [ ] Backup/sync

## Teknisk stack
- **Framework**: .NET MAUI
- **Databas**: SQLite
- **Arkitektur**: MVVM (Model-View-ViewModel)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Plattformar**: Android (primärt), iOS (sekundärt)

## Nuvarande funktioner ✅

### 1. Hem-sida (HomePage)
- Välkomstmeddelande
- Visar aktivt jobb
- Navigation till att skapa första jobb

### 2. Jobb-hantering
- **Skapa jobb** (AddJobPage):
  - Jobbtitel och arbetsplats
  - Anställningstyp (Tillsvidare/Vikarie/Behovsanställd)
  - Lönetyp (Månadslön/Timlön)
  - Förväntade timmar per månad
  - Skattesats
- **Redigera jobb** (EditJobPage):
  - Uppdatera befintlig jobbinformation
  - Förifylld data från valt jobb
- **Radera jobb**:
  - Säker radering med bekräftelse
  - Automatisk aktivering av nästa jobb

### 3. Inställningar (SettingsPage)
- Visa aktivt jobb (vid 1 jobb)
- Växla mellan jobb (vid flera jobb)
- Hantera jobb: [➕ Lägg till] [✏️ Ändra] [🗑️ Radera]
- Debug-funktioner

### 4. Databas & Data
- SQLite databas med JobProfile-modell
- CRUD-operationer (Create, Read, Update, Delete)
- Automatisk databasinitiering
- Hantering av aktivt jobb

## Projektstruktur
```
MyWorkSalary/
├── Models/
│   ├── JobProfile.cs           # Jobbprofil datamodell
│   └── EmploymentType.cs       # Anställningstyp enum
├── Views/
│   ├── HomePage.xaml           # Startsida
│   ├── SettingsPage.xaml       # Inställningar
│   └── Pages/
│       ├── AddJobPage.xaml     # Skapa nytt jobb
│       └── EditJobPage.xaml    # Redigera jobb
├── ViewModels/
│   ├── HomeViewModel.cs        # Hem-sida logik
│   ├── SettingsViewModel.cs    # Inställningar logik
│   ├── AddJobViewModel.cs      # Skapa jobb logik
│   └── EditJobViewModel.cs     # Redigera jobb logik
├── Services/
│   └── DatabaseService.cs      # Databas-operationer
├── Resources/
│   └── Styles/
│       └── Styles.xaml         # App-stilar och färger
└── Platforms/                  # Plattformsspecifik kod
```

## Utvecklingsplan

### Klart ✅
- [x] Projektplanering och UI-design
- [x] Skapa MAUI-projekt
- [x] Grundläggande datamodeller
- [x] Databas setup (SQLite)
- [x] MVVM-arkitektur med Dependency Injection
- [x] Navigation och routing
- [x] Hem-sida med dynamiskt innehåll
- [x] Komplett jobb-hantering (CRUD)
- [x] Inställningar med smart UI
- [x] Formulärvalidering och felhantering

### Pågående 🚧
- [ ] Pass-registrering (arbetstid)
- [ ] Lön-beräkningar
- [ ] Månadsöversikter

### Nästa steg 📋
1. **Pass-sida**: Registrera arbetstimmar och datum
2. **Lön-sida**: Beräkna och visa lön baserat på pass
3. **OB-tillägg**: Automatisk beräkning baserat på tid
4. **Rapporter**: Månads- och veckoöversikter

## Tekniska beslut

### Arkitektur
- **MVVM**: Separation mellan UI och logik
- **Commands**: För användarinteraktioner
- **Data Binding**: Automatisk UI-uppdatering
- **Dependency Injection**: Löst kopplad kod

### UI/UX
- **Material Design**: Moderna kort och färger
- **Responsiv design**: Fungerar på olika skärmstorlekar
- **Intuitive navigation**: Shell-baserad navigation
- **Säker UX**: Bekräftelsedialoger för destruktiva åtgärder

## Installation och körning

```bash
# Klona projektet
git clone [repository-url]

# Öppna i Visual Studio
# Välj Android/iOS emulator
# Tryck F5 för att köra
```

## Utvecklingsmiljö
- **IDE**: Visual Studio 2022
- **SDK**: .NET 8
- **Emulator**: Android API 34+
