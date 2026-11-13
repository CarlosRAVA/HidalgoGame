# 1810: El Grito de la Libertad — juego completo (menú + 3 niveles)

## Instalación
1. Copia la carpeta `Dolores_Juego` dentro de `Assets/` de tu proyecto.
   (La carpeta anterior `Assets/Dolores_Nivel1` ya no hace falta: puedes borrarla cuando quieras.
   Los scripts nuevos usan otro espacio de nombres, así que no chocan aunque la dejes.)
2. Espera a que Unity importe. Luego: **Tools > Dolores 1810 > Construir juego completo (menu + 3 niveles)**.
   Crea `MainMenu`, `Level1`, `Level2` y `Level3`, configura los ~410 sprites, crea las capas Ground/Player/Enemy,
   crea los datos (armas, tienda, enemigos) y agrega las 4 escenas al Build Settings.
   Antes de sobrescribir guarda copias de tus escenas anteriores en `Assets/Scenes/Respaldo_Anterior/`.
3. Abre `MainMenu` y presiona Play.

Otras opciones del menú Tools: **Validar reglas del juego** (comprueba la tabla de enemigos, el arma que hace el doble,
las 2 compras de corazones, las escenas y la tienda).

## Controles
- Mover A/D · Correr SHIFT (gasta resistencia) · Saltar ESPACIO · Atacar J/Z/X o clic · Interactuar E
- Trepar escaleras W/S (solo en escaleras) · Cambiar arma Q o teclas 1-5 · Pausa ESC/P
- Control: stick/cruceta, A salta, X/B ataca, LT/LB corre, Y interactúa, RB cambia arma, Start pausa.

## Reglas principales (todas configurables)
- Enemigos (ScriptableObjects en `Data/Enemies`): débil 2 golpes/1 corazón, intermedio 4/2, resistente 6/3, muy resistente 8/4
  (golpes calculados con el hacha de daño 10).
- Armas (`Data/Weapons`): Hacha 10 · Espada 15 · Machete 20 · Lanza 25 · Estoque de honor 50 (= doble que la lanza).
- Corazones: base 5, +1 por cada una de las 2 compras (máximo 7).
- Tienda (`Data/Shop`): armas, corazón adicional (2 niveles), resistencia máxima, recuperación de aliento, velocidad, ataque.
- Guardado: `Application.persistentDataPath/dolores1810.sav` (JSON firmado con HMAC; si se edita a mano se descarta).
  Se guardan monedas, armas, mejoras, niveles completados, cofres, pergaminos y misiones. Los niveles bloqueados se calculan a partir
  de los niveles completados, no de una variable editable.

## Niveles
1. El Pueblo de Dolores: día a noche. Termina en la salida del pueblo (arco con antorchas), no en la campana.
2. El Camino de la Insurgencia: noche a día. Ríos con puentes, haciendas, bosque, campamentos y un capitán realista como jefe.
3. El Grito de Independencia: día a noche. Plaza con papel picado, coronel realista como jefe final y la campana, que solo existe aquí.

## Si quieres ajustar
- Valores de enemigos/armas/precios: selecciona los assets en `Data/` (se pisan si vuelves a ejecutar el constructor).
- Jugador: objeto `Jugador` > PlatformerPlayer (velocidades, resistencia, invulnerabilidad).
- Volúmenes por defecto: `GameSettings` (se cambian en Configuración).
