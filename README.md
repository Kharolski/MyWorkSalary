# My Work Salary - Mobil App

## Projektbeskrivning
En mobil app för vårdpersonal att kontrollera om de får rätt OB-tillägg och hålla koll på timbalans.

## Målgrupp
- Primärt: Undersköterskor och vårdpersonal
- Sekundärt: Andra skiftarbetare

## Huvudfunktioner

### MVP (Minimum Viable Product)
- [ ] Skapa jobbprofiler (fast anställd vs vikarie)
- [ ] Registrera arbetspass
- [ ] Beräkna OB-tillägg automatiskt
- [ ] Visa timbalans för fast anställda
- [ ] Månadsrapporter
- [ ] Svensk helgdagskalender

### Framtida funktioner
- [ ] Sjukskrivning och VAB
- [ ] Export till PDF/Excel
- [ ] Flera jobb samtidigt
- [ ] Mörkt tema
- [ ] Backup/sync

## Teknisk stack
- **Framework**: .NET MAUI
- **Databas**: SQLite
- **Plattformar**: Android (primärt), iOS (sekundärt)

## UI-flöde
1. Välkommen/Onboarding
2. Skapa första jobbet
3. Huvudskärm med översikt
4. Lägg till arbetspass
5. Månadsrapporter

## Projektstruktur
```
MyWorkSalary/
├── Models/          # Datamodeller
├── Views/           # UI-sidor
├── ViewModels/      # MVVM logik
├── Services/        # Affärslogik
├── Data/           # Databas
└── Resources/      # Bilder, stilar
```

## Utvecklingsplan
- [x] Projektplanering och UI-design
- [x] Skapa MAUI-projekt
- [ ] Grundläggande datamodeller
- [ ] Databas setup (SQLite)
- [ ] Första UI-sida (Välkommen)
- [ ] Navigation mellan sidor
