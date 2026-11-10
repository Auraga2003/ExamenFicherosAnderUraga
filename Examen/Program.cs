using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ACB
{
    class Program
    {
        // Ruta RELATIVA del CSV (el fichero tiene que estar dentro de la carpeta bin)
        static string rutaCsv = "./acb2526.csv";

        static void Main(string[] args)
        {
            // Para que se vean bien las tildes y letras como à, ç, ñ
           

            // Leemos el CSV UNA sola vez. La línea 0 es la cabecera, los datos empiezan en la 1
            string[] lineas;
            try
            {
                lineas = File.ReadAllLines(rutaCsv);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al leer el CSV: " + ex.Message);
                return;
            }

            // ---------- MENÚ ----------
            bool salir = false;
            while (!salir)
            {
                Console.WriteLine("\n===== ACB TEMPORADA 25-26 =====");
                Console.WriteLine("1. Resultados de un equipo");
                Console.WriteLine("2. Datos de una jornada");
                Console.WriteLine("3. Top 3 MVPs");
                Console.WriteLine("4. Clasificación");
                Console.WriteLine("5. Crear un fichero por cada equipo");
                Console.WriteLine("0. Salir");
                Console.Write("Elige una opción: ");
                string opcion = Console.ReadLine();

                if (opcion == "1")
                    VerEquipo(lineas);
                else if (opcion == "2")
                    VerJornada(lineas);
                else if (opcion == "3")
                    VerMvps(lineas);
                else if (opcion == "4")
                    VerClasificacion(lineas);
                else if (opcion == "5")
                    GuardarFicherosEquipos(lineas);
                else if (opcion == "0")
                    salir = true;
                else
                    Console.WriteLine("Opción no válida.");
            }
        }

        // ============================================================
        // OPCIÓN 1: resultados de un equipo
        // ============================================================
        static void VerEquipo(string[] lineas)
        {
            Console.Write("Nombre del equipo: ");
            string equipo = Console.ReadLine().Trim();

            List<string> salida = ObtenerPartidosEquipo(lineas, equipo);

            // Si no hay partidos es que el equipo no existe
            if (salida.Count == 0)
            {
                Console.WriteLine("No se ha encontrado el equipo \"" + equipo + "\". Los equipos son:");
                List<string> equipos = ObtenerEquipos(lineas);
                foreach (string e in equipos)
                {
                    Console.WriteLine(" - " + e);
                }
                return;
            }

            salida.Insert(0, "--- Resultados de " + equipo + " ---");
            MostrarYGuardar(salida);
        }

        // ============================================================
        // OPCIÓN 2: datos de una jornada
        // ============================================================
        static void VerJornada(string[] lineas)
        {
            Console.Write("Número de jornada: ");
            string jornada = "Jornada " + Console.ReadLine().Trim();

            List<string> salida = new List<string>();
            salida.Add("--- " + jornada + " ---");

            // Recorremos los partidos (empezamos en 1 para saltar la cabecera)
            for (int i = 1; i < lineas.Length; i++)
            {
                string[] c = lineas[i].Split(',');
                if (c[0] == jornada)
                    salida.Add(FormatearPartido(c));
            }

            // Si solo está el título es que no hay partidos de esa jornada
            if (salida.Count == 1)
            {
                Console.WriteLine("No se ha encontrado la " + jornada + ".");
                return;
            }

            MostrarYGuardar(salida);
        }

        // ============================================================
        // OPCIÓN 3: top 3 de MVPs
        // ============================================================
        static void VerMvps(string[] lineas)
        {
            // Dos listas "paralelas": en la posición i están el jugador y sus veces como MVP
            List<string> nombres = new List<string>();
            List<int> veces = new List<int>();

            for (int i = 1; i < lineas.Length; i++)
            {
                string[] c = lineas[i].Split(',');
                string mvp = c[4];

                int posicion = nombres.IndexOf(mvp);
                if (posicion == -1)
                {
                    // Primera vez que sale este jugador
                    nombres.Add(mvp);
                    veces.Add(1);
                }
                else
                {
                    veces[posicion]++;
                }
            }

            List<string> salida = new List<string>();
            salida.Add("--- Top 3 MVPs ---");

            // Buscamos el máximo 3 veces
            for (int puesto = 1; puesto <= 3; puesto++)
            {
                int mejor = 0;
                for (int i = 1; i < nombres.Count; i++)
                {
                    if (veces[i] > veces[mejor])
                        mejor = i;
                }

                salida.Add(puesto + ". " + nombres[mejor] + " - " + veces[mejor] + " veces");

                // Lo marcamos con -1 para que no vuelva a salir en el siguiente puesto
                veces[mejor] = -1;
            }

            MostrarYGuardar(salida);
        }

        // ============================================================
        // OPCIÓN 4: clasificación
        // Orden: más victorias primero. DESEMPATE (criterio propio): mayor
        // diferencia de puntos (puntos a favor - puntos en contra) en toda la temporada.
        // ============================================================
        static void VerClasificacion(string[] lineas)
        {
            List<string> equipos = ObtenerEquipos(lineas);

            // Listas paralelas: en la posición i están los datos del equipo equipos[i]
            List<int> victorias = new List<int>();
            List<int> derrotas = new List<int>();
            List<int> puntosFavor = new List<int>();
            List<int> puntosContra = new List<int>();
            List<bool> usado = new List<bool>();

            // Todo empieza a 0
            for (int i = 0; i < equipos.Count; i++)
            {
                victorias.Add(0);
                derrotas.Add(0);
                puntosFavor.Add(0);
                puntosContra.Add(0);
                usado.Add(false);
            }

            // Recorremos todos los partidos y sumamos los datos
            for (int i = 1; i < lineas.Length; i++)
            {
                string[] c = lineas[i].Split(',');
                string[] marcador = c[3].Split('-');
                int puntosLocal = int.Parse(marcador[0]);
                int puntosVisitante = int.Parse(marcador[1]);

                int local = equipos.IndexOf(c[1]);
                int visitante = equipos.IndexOf(c[2]);

                puntosFavor[local] += puntosLocal;
                puntosContra[local] += puntosVisitante;
                puntosFavor[visitante] += puntosVisitante;
                puntosContra[visitante] += puntosLocal;

                if (puntosLocal > puntosVisitante)
                {
                    victorias[local]++;
                    derrotas[visitante]++;
                }
                else
                {
                    victorias[visitante]++;
                    derrotas[local]++;
                }
            }

            List<string> salida = new List<string>();
            salida.Add("--- Clasificación ---");

            // En cada puesto buscamos el mejor equipo que aún no hayamos colocado
            for (int puesto = 1; puesto <= equipos.Count; puesto++)
            {
                int mejor = -1;
                for (int i = 0; i < equipos.Count; i++)
                {
                    if (usado[i])
                        continue;

                    if (mejor == -1)
                    {
                        mejor = i;
                    }
                    else if (victorias[i] > victorias[mejor])
                    {
                        mejor = i;
                    }
                    else if (victorias[i] == victorias[mejor])
                    {
                        // Empate a victorias: desempatamos por diferencia de puntos
                        int difI = puntosFavor[i] - puntosContra[i];
                        int difMejor = puntosFavor[mejor] - puntosContra[mejor];
                        if (difI > difMejor)
                            mejor = i;
                    }
                }

                usado[mejor] = true;
                int dif = puntosFavor[mejor] - puntosContra[mejor];
                salida.Add(puesto + ". " + equipos[mejor] + " - V: " + victorias[mejor]
                    + " D: " + derrotas[mejor] + " (dif. puntos: " + dif + ")");
            }

            MostrarYGuardar(salida);
        }

        // ============================================================
        // OPCIÓN 5: un fichero .txt por cada equipo (con el nombre del equipo)
        // ============================================================
        static void GuardarFicherosEquipos(string[] lineas)
        {
            string carpeta = "./equipos";
            try
            {
                Directory.CreateDirectory(carpeta);
                Console.WriteLine("**Directorio creado exitosamente.**");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al crear el directorio: " + ex.Message);
                return;
            }

            List<string> equipos = ObtenerEquipos(lineas);
            foreach (string equipo in equipos)
            {
                List<string> salida = ObtenerPartidosEquipo(lineas, equipo);
                salida.Insert(0, "--- Resultados de " + equipo + " ---");

                string ruta = carpeta + "/" + equipo + ".txt";
                try
                {
                    File.WriteAllLines(ruta, salida);
                    Console.WriteLine("**Fichero creado: " + ruta + "**");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al crear el fichero de " + equipo + ": " + ex.Message);
                }
            }
        }

        // ============================================================
        // FUNCIONES AUXILIARES
        // ============================================================

        // Devuelve la lista de equipos sin repetir
        static List<string> ObtenerEquipos(string[] lineas)
        {
            List<string> equipos = new List<string>();
            for (int i = 1; i < lineas.Length; i++)
            {
                string[] c = lineas[i].Split(',');
                if (!equipos.Contains(c[1]))
                    equipos.Add(c[1]);
                if (!equipos.Contains(c[2]))
                    equipos.Add(c[2]);
            }
            return equipos;
        }

        // Devuelve los partidos (ya formateados) en los que juega un equipo,
        // sea como local o como visitante. No distingue mayúsculas de minúsculas.
        static List<string> ObtenerPartidosEquipo(string[] lineas, string equipo)
        {
            List<string> partidos = new List<string>();
            for (int i = 1; i < lineas.Length; i++)
            {
                string[] c = lineas[i].Split(',');
                if (c[1].ToLower() == equipo.ToLower() || c[2].ToLower() == equipo.ToLower())
                    partidos.Add(FormatearPartido(c));
            }
            return partidos;
        }

        // Convierte los campos de una línea del CSV en un texto con TODOS los datos
        // Campos: 0 jornada, 1 local, 2 visitante, 3 resultado, 4 mvp, 5 puntos, 6 asistencias, 7 rebotes, 8 equipo del mvp
        static string FormatearPartido(string[] c)
        {
            return c[0] + " | " + c[1] + " " + c[3] + " " + c[2]
                + " | MVP: " + c[4] + " (" + c[8] + ") - "
                + c[5] + " puntos, " + c[6] + " asistencias, " + c[7] + " rebotes";
        }

        // Muestra el resultado por pantalla y pregunta si se quiere guardar en un .txt
        // con el nombre que elija el usuario
        static void MostrarYGuardar(List<string> salida)
        {
            Console.WriteLine();
            foreach (string linea in salida)
            {
                Console.WriteLine(linea);
            }

            Console.Write("\n¿Quieres guardarlo en un fichero? (s/n): ");
            string respuesta = Console.ReadLine().Trim().ToLower();
            if (respuesta == "s")
            {
                Console.Write("Nombre del fichero (sin .txt): ");
                string nombre = Console.ReadLine().Trim();
                string ruta = "./" + nombre + ".txt";
                try
                {
                    File.WriteAllLines(ruta, salida);
                    Console.WriteLine("**Fichero guardado exitosamente: " + ruta + "**");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al guardar el fichero: " + ex.Message);
                }
            }
        }
    }
}
