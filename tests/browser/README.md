# Pruebas del editor en un navegador real

Estas pruebas ejecutan Chromium contra una API .NET con SQLite temporal. Nunca utilizan la base de desarrollo ni un servidor que tengas abierto. La API arranca en un puerto libre y se detiene al finalizar; los datos temporales se eliminan.

Desde la raíz, con el SDK .NET 10 y Node 24:

```bash
dotnet build Switchyard.sln --configuration Release
npm ci --prefix tests/browser
```

Instala el navegador desde esta carpeta y ejecuta las pruebas:

```bash
cd tests/browser
npx playwright install chromium
npm test
```

En Linux puede ser necesario `npx playwright install --with-deps chromium`. La CI hace estos pasos en Ubuntu. Windows conserva las pruebas .NET; el recorrido de navegador automatizado se ejecuta en Linux y se ha comprobado también en macOS.

Las pruebas cubren dibujo en ambos sentidos, confirmación explícita, persistencia tras recargar, coordenadas manuales, solapamientos, cancelación, fallo de red, foco y selección durante consultas, interacción táctil, planos de 50 × 50 m y conflictos con otro cliente. Axe comprueba un conjunto de reglas WCAG A/AA en la vista móvil; no sustituye una auditoría manual completa ni una prueba con un teléfono físico.

Node, Playwright y axe son herramientas de desarrollo. El usuario de Switchyard sigue necesitando solo .NET y un navegador para ejecutar la aplicación.
