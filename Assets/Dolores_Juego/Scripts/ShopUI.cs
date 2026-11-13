using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dolores1810
{
    /// <summary>
    /// Tienda: armas y mejoras. Se usa dentro de los niveles (carretas) y desde el menú principal. Todas las compras
    /// pasan por PlayerProgress, que valida el precio y los límites y guarda el resultado.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        class Entry
        {
            public ShopItemData item;
            public WeaponData weapon;
            public Button button;
            public Text nameText, stateText;
            public Image icon, back;
        }

        readonly List<Entry> entries = new List<Entry>();
        Text coinsText, detailTitle, detailBody, message;
        Button closeButton;
        int selected;
        float messageTimer;
        System.Action onClose;
        public System.Action onPurchased;

        public static ShopUI Create(Transform parent, System.Action onClose)
        {
            var overlay = UIFactory.FullScreen(parent, "Tienda", new Color(0f, 0f, 0f, 0.7f));
            overlay.raycastTarget = true;
            var ui = overlay.gameObject.AddComponent<ShopUI>();
            ui.onClose = onClose;
            ui.Build();
            return ui;
        }

        void Build()
        {
            Transform t = transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1560f, 960f));
            var title = UIFactory.Label(t, "Titulo", "TIENDA DEL MERCADER", 78, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-120f, 405f), new Vector2(1000f, 100f));
            coinsText = UIFactory.Label(t, "Monedas", "", 46, new Color(0.55f, 0.32f, 0.02f, 1f), TextAnchor.MiddleRight, FontStyle.Bold, false);
            UIFactory.PlaceCenter(coinsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 405f), new Vector2(420f, 70f));

            var db = GameDatabase.I;
            if (db != null && db.shopItems != null)
            {
                for (int i = 0; i < db.shopItems.Length; i++)
                {
                    var item = db.shopItems[i];
                    if (item == null) continue;
                    WeaponData w = item.kind == ShopKind.Weapon ? db.GetWeapon(item.weaponId) : null;
                    if (item.kind == ShopKind.Weapon && (w == null || w.price <= 0)) continue;
                    AddCard(item, w);
                }
            }

            var detailBack = UIFactory.Box(t, "DetalleFondo", new Color(0.3f, 0.18f, 0.08f, 0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1440f, 190f));
            UIFactory.PlaceCenter(detailBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -310f), new Vector2(1440f, 190f));
            detailTitle = UIFactory.Label(t, "DetalleTitulo", "", 44, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Bold, false);
            UIFactory.PlaceCenter(detailTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-190f, -258f), new Vector2(1000f, 56f));
            detailBody = UIFactory.Label(t, "DetalleTexto", "", 32, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Normal, false);
            UIFactory.PlaceCenter(detailBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-190f, -335f), new Vector2(1000f, 120f));
            message = UIFactory.Label(t, "Mensaje", "", 38, new Color(0.65f, 0.1f, 0.06f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(1000f, 50f));
            closeButton = UIFactory.MakeButton(t, "Cerrar", new Vector2(560f, -330f), new Vector2(320f, 90f), Close);
        }

        void AddCard(ShopItemData item, WeaponData w)
        {
            int idx = entries.Count;
            int col = idx % 3, row = idx / 3;
            Vector2 center = new Vector2(-470f + col * 470f, 255f - row * 170f);
            var b = UIFactory.MakeButton(transform, "", center, new Vector2(450f, 150f), () => Activate(idx));
            var lbl = b.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = "";
            var e = new Entry();
            e.item = item; e.weapon = w; e.button = b;
            e.back = b.GetComponent<Image>();
            e.icon = UIFactory.Icon(b.transform, "Icono", item.icon, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(104f, 104f));
            e.icon.rectTransform.pivot = new Vector2(0f, 0.5f);
            e.nameText = UIFactory.Label(b.transform, "Nombre", item.displayName, 32, UIFactory.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(e.nameText.rectTransform, new Vector2(0f, 1f), new Vector2(136f, -16f), new Vector2(300f, 50f));
            e.stateText = UIFactory.Label(b.transform, "Estado", "", 28, UIFactory.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(e.stateText.rectTransform, new Vector2(0f, 1f), new Vector2(136f, -66f), new Vector2(300f, 70f));
            var card = b.gameObject.AddComponent<ShopCard>();
            card.owner = this; card.index = idx;
            entries.Add(e);
        }

        void OnEnable()
        {
            Refresh();
            if (entries.Count > 0) { UIFactory.Select(entries[0].button); ShowDetail(0); }
        }

        void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f && message != null) message.text = "";
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        void Close()
        {
            gameObject.SetActive(false);
            if (onClose != null) onClose();
        }

        string StateOf(Entry e, out bool canAct)
        {
            canAct = false;
            if (e.item.kind == ShopKind.Weapon)
            {
                if (PlayerProgress.OwnsWeapon(e.weapon.id))
                {
                    bool eq = PlayerProgress.EquippedWeaponId == e.weapon.id;
                    canAct = !eq;
                    return eq ? "EQUIPADA" : "COMPRADA - equipar";
                }
                canAct = true;
                return e.weapon.price + " monedas";
            }
            int lvl = PlayerProgress.GetUpgradeLevel(e.item.kind);
            int max = PlayerProgress.MaxUpgradeLevel(e.item);
            if (lvl >= max) return "NIVEL MÁXIMO (" + lvl + "/" + max + ")";
            canAct = true;
            return "Nivel " + lvl + "/" + max + " - " + e.item.prices[lvl] + " monedas";
        }

        public void Refresh()
        {
            if (coinsText != null) coinsText.text = "Monedas: " + PlayerProgress.Coins;
            for (int i = 0; i < entries.Count; i++)
            {
                bool can;
                string s = StateOf(entries[i], out can);
                entries[i].stateText.text = s;
                bool affordable = can && PriceOf(entries[i]) <= PlayerProgress.Coins;
                entries[i].stateText.color = can ? (affordable ? UIFactory.Gold : new Color(1f, 0.55f, 0.5f, 1f)) : new Color(0.7f, 1f, 0.7f, 1f);
            }
            ShowDetail(selected);
        }

        int PriceOf(Entry e)
        {
            if (e.item.kind == ShopKind.Weapon)
                return PlayerProgress.OwnsWeapon(e.weapon.id) ? 0 : e.weapon.price;
            int lvl = PlayerProgress.GetUpgradeLevel(e.item.kind);
            if (lvl >= PlayerProgress.MaxUpgradeLevel(e.item)) return 0;
            return e.item.prices[lvl];
        }

        public void ShowDetail(int index)
        {
            if (index < 0 || index >= entries.Count) return;
            selected = index;
            var e = entries[index];
            detailTitle.text = e.item.displayName;
            string extra = "";
            if (e.item.kind == ShopKind.Weapon)
                extra = "\nDaño " + e.weapon.damage + " · Velocidad x" + e.weapon.attackSpeed.ToString("0.0") + " · Alcance x" + e.weapon.reach.ToString("0.0");
            detailBody.text = e.item.description + extra;
        }

        void Activate(int index)
        {
            if (index < 0 || index >= entries.Count) return;
            var e = entries[index];
            bool ok = false;
            if (e.item.kind == ShopKind.Weapon)
            {
                if (PlayerProgress.OwnsWeapon(e.weapon.id))
                {
                    if (PlayerProgress.EquippedWeaponId == e.weapon.id) { Say("Ya la llevas equipada."); return; }
                    ok = PlayerProgress.Equip(e.weapon.id);
                    if (ok)
                    {
                        AudioHub.Play(SfxId.WeaponSwitch, 0f);
                        var pl = GameSession.I != null ? GameSession.I.player : null;
                        if (pl != null) pl.EquipById(e.weapon.id, false);
                        Say("Equipada: " + e.weapon.displayName);
                    }
                }
                else
                {
                    if (PlayerProgress.Coins < e.weapon.price) { Fail(); return; }
                    ok = PlayerProgress.TryBuyWeapon(e.item, e.weapon);
                    if (ok)
                    {
                        AudioHub.Play(SfxId.Buy, 0f);
                        var pl = GameSession.I != null ? GameSession.I.player : null;
                        if (pl != null) pl.EquipById(e.weapon.id, false);
                        Say("¡Compraste " + e.weapon.displayName + "!");
                    }
                    else Fail();
                }
            }
            else
            {
                int lvl = PlayerProgress.GetUpgradeLevel(e.item.kind);
                if (lvl >= PlayerProgress.MaxUpgradeLevel(e.item)) { Say("Ya está al máximo."); AudioHub.Play(SfxId.Error, 0f); return; }
                if (PlayerProgress.Coins < e.item.prices[lvl]) { Fail(); return; }
                ok = PlayerProgress.TryBuyUpgrade(e.item);
                if (ok)
                {
                    AudioHub.Play(SfxId.Buy, 0f);
                    Say("¡Mejora comprada!");
                    var pl = GameSession.I != null ? GameSession.I.player : null;
                    if (pl != null) pl.RefreshStats(true);
                }
                else Fail();
            }
            if (ok && onPurchased != null) onPurchased();
            Refresh();
            if (GameSession.I != null) GameSession.I.RaiseStats();
        }

        void Fail()
        {
            AudioHub.Play(SfxId.Error, 0f);
            Say("No tienes suficientes monedas.");
        }

        void Say(string s)
        {
            if (message != null) message.text = s;
            messageTimer = 2.2f;
        }
    }
}
