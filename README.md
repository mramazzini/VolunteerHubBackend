# SixSeven – Local Development

## Run the API
```bash
dotnet restore
dotnet run --project SixSeven.API
```

---

## Run Tests (with Coverage)
```bash
dotnet test \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura
```

---

## Generate Coverage Report
Install once:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Generate report:
```bash
reportgenerator \
  -reports:**/coverage.cobertura.xml \
  -targetdir:coverage-report \
  -reporttypes:Html;TextSummary
```

---

## View Coverage Report
```bash
xdg-open coverage-report/index.html
```

**Report file:** `coverage-report/index.html`
