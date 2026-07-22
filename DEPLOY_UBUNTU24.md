# Despliegue de ServPersonalCtr en Ubuntu 24 con Docker

## 1. Hallazgos del backend

- El proyecto es una API `ASP.NET Core` sobre `.NET 10`.
- Usa PostgreSQL mediante funciones almacenadas (`fn_setseccion`, `fn_validateseccion`, `fn_getpersona`, `fn_getlicencia`, entre otras).
- Guarda soportes documentales en disco y luego los respalda en Google Drive.
- Consume Gemini para analizar licencias en PNG.
- El backend necesita variables sensibles fuera del repositorio: base de datos, Gemini y Google Drive.

## 2. Riesgos detectados antes de subirlo

- El `appsettings.json` tenia una cadena de conexion con credenciales reales; ahora queda lista para inyectarse por variables de entorno.
- La ruta `\filesoporte\` era de estilo Windows y en Linux puede fallar o terminar en una ruta incorrecta.
- `UseHttpsRedirection()` sin `ForwardedHeaders` puede provocar redirecciones incorrectas cuando la API corre detras de un proxy TLS.
- El respaldo SQL `respaldo_completo.sql` quedo desactualizado (le faltaban `fn_getsoportedoc`, `fn_updatelicencia`, `fn_getlicenciaini` y `fn_getlicenciasactivas`, y su `fn_setlicencias` no incluia `p_soportes jsonb`). Usar `respaldo_completo_2026-07-21.sql`, generado con `pg_dump` desde la base de produccion el 2026-07-21 y verificado contra las llamadas del backend (sin incongruencias).

## 3. Arquitectura recomendada

- `app`: contenedor de la API en puerto interno `8080`.
- `caddy`: proxy reverso en `80/443`, emite y renueva certificados automaticamente.
- `volumen app_soportes`: persistencia de `/app/filesoporte`.
- PostgreSQL externo: el contenedor se conecta usando `ConnectionStrings__PostgresConnection`.

## 4. Certificado confiable

- Si quieres decirle al cliente que el certificado es confiable sin advertencias, debes usar un dominio publico real como `api.midominio.com` apuntando al servidor y un certificado emitido por una CA publica.
- En esta guia uso `Caddy` porque genera y renueva certificados automaticamente con Let's Encrypt o ZeroSSL.
- Si el sistema solo vivira en red interna o por IP, puedes generar un certificado interno, pero no sera confiable por defecto. En ese caso solo sera confiable despues de instalar la CA raiz en cada equipo cliente.

## 5. Preparar Ubuntu 24

```bash
sudo apt update
sudo apt install -y ca-certificates curl gnupg
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin git
sudo systemctl enable docker
sudo systemctl start docker
```

## 6. Publicar el codigo en el servidor

```bash
cd /opt
sudo git clone <URL-DEL-REPOSITORIO> servpersonalctr
sudo chown -R $USER:$USER /opt/servpersonalctr
cd /opt/servpersonalctr
cp .env.production.example .env.production
chmod +x deploy/deploy.sh
```

## 7. Configurar variables

Edita `.env.production` y completa al menos:

- `DOMAIN`: dominio publico que apuntara al servidor.
- `ConnectionStrings__PostgresConnection`: conexion real a PostgreSQL.
- `Gemini__ApiKey`: clave real para el módulo de análisis de licencias.
- `GoogleDrive__ClientEmail` y `GoogleDrive__PrivateKey`: credenciales del service account si usaras respaldo en Drive.
- `Cors__AllowedOrigins__0`: URL exacta del frontend, por ejemplo `https://app.midominio.com`.

## 8. DNS y puertos

- Crea un registro `A` o `AAAA` para `DOMAIN` apuntando al servidor.
- Abre `80/tcp` y `443/tcp` en firewall y proveedor cloud.
- Si usas `ufw`:

```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

## 9. Primer despliegue

```bash
cd /opt/servpersonalctr
./deploy/deploy.sh
docker compose --env-file .env.production -f docker-compose.prod.yml logs -f
```

Valida luego:

```bash
curl -I https://api.midominio.com
docker compose --env-file .env.production -f docker-compose.prod.yml ps
```

## 10. Actualizar con nuevas modificaciones

Cada vez que subas cambios:

```bash
cd /opt/servpersonalctr
git pull
./deploy/deploy.sh
```

Eso recompila la API, recrea contenedores y mantiene el volumen persistente de soportes.

## 11. Base de datos

Antes de habilitar produccion:

1. Restaura o aplica el esquema de PostgreSQL usando el respaldo mas reciente (`respaldo_completo_2026-07-21.sql`).
2. Verifica que existan las funciones requeridas por el codigo actual.
3. Confirma que `fn_setlicencias` acepte tambien la estructura de soportes que hoy envia el backend (el respaldo del 2026-07-21 ya la incluye con `p_soportes jsonb`).
4. Prueba manualmente `Login`, `Personal/Get`, `Licencias/Set` y `Licencias/Get`.

## 12. Si no tienes dominio publico

- Puedes reemplazar el `Caddyfile` para usar `tls internal`.
- Eso genera un certificado interno valido tecnicamente, pero no confiable para navegadores o clientes externos hasta distribuir la CA raiz.
- En ese escenario no debes decirle al cliente "es confiable" sin aclarar que requiere instalar la CA corporativa.
