\# Bruktbørsen



Porteføljeoppgave del 1 i DTE2802 – Webapplikasjoner 2 ved UiT - Ssv031.



Prosjektet er et REST API for en markedsplass for brukte varer, utviklet med ASP.NET Core og .NET 10. API-et skal senere brukes som backend for en Blazor-applikasjon i del 2 av oppgaven.

Dokumentasjon er fin skrevet og omstrukturert ved hjelp av Ai-verktøy, forbeholder mulige feil.



\## Funksjonalitet



\- Registrering og innlogging av brukere.

\- Opprettelse, endring og fjerning av annonser.

\- Søk, filtrering og sortering.

\- Opprettelse og håndtering av bud.

\- Favoritter og prishistorikk.

\- Prisforslag, prisvurdering og markedsstatistikk.

\- Rollebasert tilgangskontroll.



\## Nødvendig programvare



\- \[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

\- Entity Framework Core CLI 10.0.0

\- Nettleser

\- \[Git](https://git-scm.com/downloads) dersom prosjektet skal klones.



Prosjektet er utviklet på Windows med JetBrains Rider, men krever ikke et eget utviklingsmiljø for å kjøres.



SQLite benyttes som database, og det er derfor ikke nødvendig å installere en separat databaseserver.



\## Installasjon og oppstart



Kommandoene nedenfor er beregnet for Windows CMD.



\### 1. Last ned prosjektet



Pakk ut den innleverte ZIP-filen eller klon prosjektet fra GitHub:



```cmd

git clone https://github.com/knommonk5/Dte2800WebappP1.git Bruktborsen

cd Bruktborsen

git switch develop

```



Alle videre kommandoer kjøres fra prosjektroten, hvor `Bruktborsen.slnx` ligger.



\### 2. Kontroller .NET-versjon



```cmd

dotnet --list-sdks

```



.NET 10 SDK må være installert.



\### 3. Installer EF Core CLI



```cmd

dotnet tool install --global dotnet-ef --version 10.0.0

```



Dersom verktøyet allerede er installert, kan versjonen kontrolleres med:



```cmd

dotnet ef --version

```



\### 4. Gjenopprett avhengigheter og bygg prosjektet



```cmd

dotnet restore Bruktborsen.slnx

dotnet build Bruktborsen.slnx

```



\### 5. Opprett databasen



```cmd

dotnet ef database update --project Bruktborsen.Api

```



Dette oppretter SQLite-databasen `bruktborsen.db` og anvender eksisterende migrasjoner.



\### 6. Godkjenn HTTPS-sertifikat



```cmd

dotnet dev-certs https --trust

```



\### 7. Aktiver seed-data



```cmd

set SeedData\_\_Enabled=true

```



Dette oppretter demonstrasjonsdata ved oppstart av API-et.



\### 8. Start API-et



```cmd

dotnet run --project Bruktborsen.Api --launch-profile https

```



API-et er tilgjengelig på:



\- https://localhost:7089

\- http://localhost:5055



Åpne Scalar i nettleseren for å teste API-et:



https://localhost:7089/scalar/v1



OpenAPI-spesifikasjonen finnes på:



https://localhost:7089/openapi/v1.json



Scalar og seed-data er kun tilgjengelig i utviklingsmiljøet.



\## Demonstrasjonsbrukere



Følgende brukere opprettes når seed-data aktiveres:



| E-post | Passord | Rolle |

|---|---|---|

| admin@brukt.no | Brukt2026! | Admin |

| ola@brukt.no | Brukt2026! | User |

| kari@brukt.no | Brukt2026! | User |

| per@brukt.no | Brukt2026! | User |



Disse brukerne er kun beregnet for lokal testing og demonstrasjon. Seed-data bør benyttes på en egen lokal demonstrasjonsdatabase.



\## Innlogging i Scalar



Åpne `POST /api/auth/login`.



Sett `useCookies` og `useSessionCookies` til `false`.



Eksempel på innlogging:



```json

{

&#x20; "email": "ola@brukt.no",

&#x20; "password": "Brukt2026!"

}

```



Kopier `accessToken` fra responsen og legg dette inn under Bearer-autentisering i Scalar.



Innloggingen kan kontrolleres med `GET /api/auth/me`.



\## Seed-data



På en tom database opprettes:



\- 4 brukere.

\- 4 kategorier.

\- 40 annonser, hvorav 20 er solgt.

\- 56 bud fordelt på alle budstatuser.

\- 20 prisendringer.

\- 8 favoritter.



Kategoriene Elektronikk, Sykkel og Møbler har tilstrekkelig antall salg for prisvurdering, mens Friluft kun har ett salg.



Annonsen `Brukt bærbar PC - 12` er bevisst overpriset for å demonstrere prisvurderingen.



Seed-data skal normalt ikke dupliseres ved omstart.



\## Testing



Prosjektet inneholder et eget xUnit-testprosjekt, `Bruktborsen.Tests`, med enhetstester og integrasjonstester.



Stopp API-et med Ctrl + C og kjør:



```cmd

set SeedData\_\_Enabled=

dotnet test Bruktborsen.slnx

```



For å kjøre testene i Release:



```cmd

dotnet test Bruktborsen.slnx -c Release

```



\## Prosjektstruktur



```text

Bruktborsen/

&#x20;   Bruktborsen.Api/

&#x20;       Controllers/

&#x20;       DTOs/

&#x20;       Models/

&#x20;       Repositories/

&#x20;       Services/

&#x20;       Data/

&#x20;   Bruktborsen.Tests/

&#x20;   Bruktborsen.slnx

&#x20;   README.md

```



Prosjektet benytter en lagdelt arkitektur:



Controller → Service → Repository → Database



Interfaces og dependency injection benyttes mellom de ulike lagene.



\## CORS



Tillatte adresser for Blazor-klienten er konfigurert i `appsettings.Development.json`.



\- https://localhost:7100

\- http://localhost:5100



Disse må eventuelt endres i forbindelse med utviklingen av Blazor-klienten i del 2.



\## Feilsøking



Dersom databasen mangler tabeller:



```cmd

dotnet ef database update --project Bruktborsen.Api

```



Dersom Scalar ikke er tilgjengelig, kontroller at API-et er startet med HTTPS-profilen.



Dersom demonstrasjonsdata mangler, kontroller at `SeedData\_\_Enabled` er satt til `true` før oppstart.



API-et avsluttes med Ctrl + C.



Seed-data kan deaktiveres med:



```cmd

set SeedData\_\_Enabled=

```



\## Dokumentasjon og utvikling



Prosjektet er gjennomført individuelt med bruk av GitHub, issues, branches og pull requests.



Mer informasjon om arkitektur, forretningsregler, analysemodulen, testing og utviklingsprosessen finnes i prosjektdokumentasjonen.



Repository:

https://github.com/knommonk5/Dte2800WebappP1

