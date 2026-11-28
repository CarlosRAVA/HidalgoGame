using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Dolores1810
{
    /// <summary>
    /// Guardado en archivo JSON con firma HMAC-SHA256. Si alguien edita el archivo a mano la firma deja de coincidir
    /// y se usa el respaldo (.bak) o una partida nueva. Escritura atómica: se escribe un temporal y luego se reemplaza.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "dolores1810.sav";
        static readonly byte[] Key = Encoding.UTF8.GetBytes("Dolores-1810-El-Grito-de-la-Libertad-clave-de-guardado");

        static string FilePath { get { return Path.Combine(Application.persistentDataPath, FileName); } }

        static string Sign(string json)
        {
            using (var h = new HMACSHA256(Key))
            {
                byte[] sig = h.ComputeHash(Encoding.UTF8.GetBytes(json));
                return BitConverter.ToString(sig).Replace("-", "");
            }
        }

        static bool TryRead(string path, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                string[] lines = File.ReadAllLines(path);
                if (lines.Length < 2) return false;
                if (Sign(lines[0]) != lines[1].Trim()) return false;
                data = JsonUtility.FromJson<SaveData>(lines[0]);
                return data != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasSave()
        {
            SaveData d;
            return TryRead(FilePath, out d) || TryRead(FilePath + ".bak", out d);
        }

        public static SaveData Load()
        {
            SaveData d;
            if (TryRead(FilePath, out d)) return Sanitize(d);
            if (TryRead(FilePath + ".bak", out d)) return Sanitize(d);
            if (File.Exists(FilePath)) Debug.LogWarning("[1810] El archivo de guardado no es válido; se inicia una partida nueva.");
            return new SaveData();
        }

        public static void Save(SaveData d)
        {
            try
            {
                string json = JsonUtility.ToJson(Sanitize(d));
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json + "\n" + Sign(json));
                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, FilePath + ".bak", true);
                    File.Delete(FilePath);
                }
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[1810] No se pudo guardar: " + e.Message);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                if (File.Exists(FilePath + ".bak")) File.Delete(FilePath + ".bak");
            }
            catch (Exception) { }
        }

        /// <summary>Corrige valores imposibles (negativos, repetidos, fuera de rango).</summary>
        public static SaveData Sanitize(SaveData d)
        {
            if (d == null) return new SaveData();
            d.coins = Mathf.Clamp(d.coins, 0, 999999);
            d.heartsLevel = Mathf.Clamp(d.heartsLevel, 0, GameRules.MaxHeartUpgrades);
            d.staminaCapLevel = Mathf.Clamp(d.staminaCapLevel, 0, 3);
            d.staminaRecLevel = Mathf.Clamp(d.staminaRecLevel, 0, 3);
            d.speedLevel = Mathf.Clamp(d.speedLevel, 0, 3);
            d.attackLevel = Mathf.Clamp(d.attackLevel, 0, 3);
            d.shieldLevel = Mathf.Clamp(d.shieldLevel, 0, GameRules.MaxShieldUpgrades);
            d.lastLevel = Mathf.Clamp(d.lastLevel, 1, 4);
            if (d.ownedWeapons == null) d.ownedWeapons = new System.Collections.Generic.List<string>();
            if (!d.ownedWeapons.Contains("hacha")) d.ownedWeapons.Insert(0, "hacha");
            var unique = new System.Collections.Generic.List<string>();
            foreach (string w in d.ownedWeapons) if (!string.IsNullOrEmpty(w) && !unique.Contains(w)) unique.Add(w);
            d.ownedWeapons = unique;
            if (string.IsNullOrEmpty(d.equippedWeapon) || !d.ownedWeapons.Contains(d.equippedWeapon)) d.equippedWeapon = "hacha";
            if (d.completedLevels == null) d.completedLevels = new System.Collections.Generic.List<int>();
            var lv = new System.Collections.Generic.List<int>();
            foreach (int l in d.completedLevels) if (l >= 1 && l <= 4 && !lv.Contains(l)) lv.Add(l);
            d.completedLevels = lv;
            if (d.openedChests == null) d.openedChests = new System.Collections.Generic.List<string>();
            if (d.foundCollectibles == null) d.foundCollectibles = new System.Collections.Generic.List<string>();
            if (d.doneMissions == null) d.doneMissions = new System.Collections.Generic.List<string>();
            if (d.levelKills == null || d.levelKills.Length != 4) d.levelKills = ResizeIntArray(d.levelKills, 4);
            if (d.levelCoins == null || d.levelCoins.Length != 4) d.levelCoins = ResizeIntArray(d.levelCoins, 4);
            if (d.levelScores == null || d.levelScores.Length != 4) d.levelScores = ResizeIntArray(d.levelScores, 4);
            if (d.levelTimes == null || d.levelTimes.Length != 4) d.levelTimes = ResizeFloatArray(d.levelTimes, 4);
            if (d.levelBreaks == null || d.levelBreaks.Length != 4) d.levelBreaks = ResizeIntArray(d.levelBreaks, 4);
            if (d.levelMissions == null || d.levelMissions.Length != 4) d.levelMissions = ResizeIntArray(d.levelMissions, 4);
            return d;
        }

        static int[] ResizeIntArray(int[] src, int len)
        {
            var dst = new int[len];
            if (src != null)
            {
                int n = Mathf.Min(src.Length, len);
                for (int i = 0; i < n; i++) dst[i] = Mathf.Max(0, src[i]);
            }
            return dst;
        }

        static float[] ResizeFloatArray(float[] src, int len)
        {
            var dst = new float[len];
            if (src != null)
            {
                int n = Mathf.Min(src.Length, len);
                for (int i = 0; i < n; i++) dst[i] = Mathf.Max(0f, src[i]);
            }
            return dst;
        }
    }
}
