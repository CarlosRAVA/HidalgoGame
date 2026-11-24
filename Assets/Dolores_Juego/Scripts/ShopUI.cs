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
            public Text nameText, subText, stateText;
            public Image icon, back, stateBadge;
            public GameObject selectBorder;
        }

        readonly List<Entry> entries = new List<Entry>();
        Text coinsText, detailTitle, detailBody, message;
        Image detailIcon;
        Button closeButton;
        int selected;
        float messageTimer;
        System.Action onClose;
        public System.Action onPurchased;

        public static ShopUI Create(Transform parent, System.Action onClose)
        {
            var overlay = UIFactory.FullScreen(parent, "Tienda", new Color(0f, 0f, 0f, 0.74f));
            overlay.raycastTarget = true;
            var ui = overlay.gameObject.AddComponent<ShopUI>();
            ui.onClose = onClose;
            ui.Build();
            return ui;
        }

        void Build()
        {
            Transform t = transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1680f, 980f));

            var title = UIFactory.Label(t, "Titulo", "TIENDA DEL MERCADER", 68, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-240f, 418f), new Vector2(1060f, 90f));

            var coinBadge = UIFactory.Box(t, "MonedasFondo", new Color(0.24f, 0.14f, 0.07f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(390f, 68f));
            UIFactory.PlaceCenter(coinBadge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(585f, 418f), new Vector2(390f, 68f));
            var coinTrim = UIFactory.Box(coinBadge.transform, "Borde", new Color(0.86f, 0.67f, 0.25f, 0.85f), new Vector2(0f, 0f), Vector2.zero, new Vector2(6f, 68f));
            UIFactory.Place(coinTrim.rectTransform, new Vector2(0f, 0f), Vector2.zero, new Vector2(6f, 68f));
            coinsText = UIFactory.Label(coinBadge.transform, "Monedas", "", 38, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Stretch(coinsText.rectTransform);

            var db = GameDatabase.I;
            if (db != null)
            {
                db.EnsureUpgrades();
                if (db.shopItems != null)
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
            }

            message = UIFactory.Label(t, "Mensaje", "", 32, new Color(0.62f, 0.1f, 0.06f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -248f), new Vector2(1500f, 44f));

            var detailBack = UIFactory.Box(t, "DetalleFondo", new Color(0.22f, 0.13f, 0.06f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1220f, 165f));
            UIFactory.PlaceCenter(detailBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-170f, -362f), new Vector2(1220f, 165f));
            var detailTrim = UIFactory.Box(detailBack.transform, "BordeSuperior", new Color(0.86f, 0.67f, 0.25f, 0.8f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1220f, 4f));
            UIFactory.Place(detailTrim.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(1220f, 4f));

            var detailIconFrame = UIFactory.Box(detailBack.transform, "DetalleIconoFondo", new Color(0.14f, 0.08f, 0.04f, 0.9f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(124f, 124f));
            UIFactory.PlaceCenter(detailIconFrame.rectTransform, new Vector2(0f, 0.5f), new Vector2(82f, 0f), new Vector2(124f, 124f));
            detailIcon = UIFactory.Icon(detailIconFrame.transform, "DetalleIcono", null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));
            UIFactory.PlaceCenter(detailIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));

            detailTitle = UIFactory.Label(detailBack.transform, "DetalleTitulo", "", 36, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UIFactory.Place(detailTitle.rectTransform, new Vector2(0f, 1f), new Vector2(166f, -14f), new Vector2(1030f, 44f));
            detailBody = UIFactory.Label(detailBack.transform, "DetalleTexto", "", 27, UIFactory.Cream, TextAnchor.UpperLeft, FontStyle.Normal, false);
            UIFactory.Place(detailBody.rectTransform, new Vector2(0f, 1f), new Vector2(166f, -62f), new Vector2(1030f, 94f));

            closeButton = UIFactory.MakeButton(t, "Cerrar", new Vector2(615f, -362f), new Vector2(310f, 100f), Close);
        }

        void AddCard(ShopItemData item, WeaponData w)
        {
            int idx = entries.Count;
            int col = idx % 2, row = idx / 2;
            Vector2 center = new Vector2(-395f + col * 790f, 285f - row * 114f);
            var b = UIFactory.MakeButton(transform, "", center, new Vector2(760f, 104f), () => Activate(idx));
            var lbl = b.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = "";

            var e = new Entry();
            e.item = item;
            e.weapon = w;
            e.button = b;
            e.back = b.GetComponent<Image>();

            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.58f, 0.42f, 0.28f, 1f);
            cb.selectedColor = new Color(0.50f, 0.35f, 0.22f, 1f);
            cb.pressedColor = new Color(0.38f, 0.25f, 0.15f, 1f);
            b.colors = cb;

            var selRoot = UIFactory.Rect("BordeSeleccion", b.transform);
            UIFactory.Stretch(selRoot);
            UIFactory.Box(selRoot, "Arriba", UIFactory.Gold, new Vector2(0f, 1f), Vector2.zero, new Vector2(760f, 3.5f));
            UIFactory.Box(selRoot, "Abajo", UIFactory.Gold, new Vector2(0f, 0f), Vector2.zero, new Vector2(760f, 3.5f));
            UIFactory.Box(selRoot, "Izq", UIFactory.Gold, new Vector2(0f, 0f), Vector2.zero, new Vector2(6f, 104f));
            UIFactory.Box(selRoot, "Der", UIFactory.Gold, new Vector2(1f, 0f), Vector2.zero, new Vector2(6f, 104f));
            e.selectBorder = selRoot.gameObject;
            e.selectBorder.SetActive(false);

            var iconFrame = UIFactory.Box(b.transform, "IconoFondo", new Color(0.16f, 0.08f, 0.04f, 0.88f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(82f, 82f));
            UIFactory.PlaceCenter(iconFrame.rectTransform, new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(82f, 82f));
            e.icon = UIFactory.Icon(iconFrame.transform, "Icono", item.icon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
            UIFactory.PlaceCenter(e.icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));

            e.nameText = UIFactory.Label(b.transform, "Nombre", item.displayName, 30, UIFactory.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UIFactory.Place(e.nameText.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -12f), new Vector2(430f, 42f));

            e.subText = UIFactory.Label(b.transform, "Subtitulo", "", 22, new Color(0.95f, 0.86f, 0.7f, 0.95f), TextAnchor.MiddleLeft, FontStyle.Normal, false);
            UIFactory.Place(e.subText.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -56f), new Vector2(430f, 34f));

            e.stateBadge = UIFactory.Box(b.transform, "EstadoFondo", new Color(0.16f, 0.08f, 0.03f, 0.9f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(204f, 72f));
            UIFactory.PlaceCenter(e.stateBadge.rectTransform, new Vector2(1f, 0.5f), new Vector2(-116f, 0f), new Vector2(204f, 72f));
            e.stateText = UIFactory.Label(e.stateBadge.transform, "Estado", "", 25, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Stretch(e.stateText.rectTransform);

            var card = b.gameObject.AddComponent<ShopCard>();
            card.owner = this;
            card.index = idx;
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

        string SubInfoOf(Entry e)
        {
            if (e.item.kind == ShopKind.Weapon && e.weapon != null)
                return "Daño " + e.weapon.damage + " · Vel x" + e.weapon.attackSpeed.ToString("0.0") + " · Alcance x" + e.weapon.reach.ToString("0.0");
            int lvl = PlayerProgress.GetUpgradeLevel(e.item.kind);
            int max = PlayerProgress.MaxUpgradeLevel(e.item);
            if (lvl >= max) return "Mejora al máximo (" + lvl + "/" + max + ")";
            return "Nivel actual: " + lvl + " / " + max;
        }

        string StateOf(Entry e, out bool canAct, out bool equippedOrMax)
        {
            canAct = false;
            equippedOrMax = false;
            if (e.item.kind == ShopKind.Weapon)
            {
                if (PlayerProgress.OwnsWeapon(e.weapon.id))
                {
                    bool eq = PlayerProgress.EquippedWeaponId == e.weapon.id;
                    canAct = !eq;
                    equippedOrMax = eq;
                    return eq ? "EQUIPADA" : "EQUIPAR";
                }
                canAct = true;
                return e.weapon.price + " monedas";
            }
            int lvl = PlayerProgress.GetUpgradeLevel(e.item.kind);
            int max = PlayerProgress.MaxUpgradeLevel(e.item);
            if (lvl >= max)
            {
                equippedOrMax = true;
                return "MÁXIMO (" + lvl + "/" + max + ")";
            }
            canAct = true;
            return e.item.prices[lvl] + " monedas";
        }

        public void Refresh()
        {
            if (coinsText != null) coinsText.text = "Monedas: " + PlayerProgress.Coins;
            for (int i = 0; i < entries.Count; i++)
            {
                bool can, done;
                string s = StateOf(entries[i], out can, out done);
                entries[i].subText.text = SubInfoOf(entries[i]);
                entries[i].stateText.text = s;
                bool affordable = can && PriceOf(entries[i]) <= PlayerProgress.Coins;
                entries[i].stateText.color = done
                    ? new Color(0.55f, 0.96f, 0.55f, 1f)
                    : (affordable ? UIFactory.Gold : new Color(1f, 0.55f, 0.5f, 1f));
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
            for (int i = 0; i < entries.Count; i++)
            {
                bool isSel = (i == selected);
                if (entries[i].back != null)
                    entries[i].back.color = isSel ? new Color(0.45f, 0.28f, 0.16f, 1f) : Color.white;
                if (entries[i].selectBorder != null)
                    entries[i].selectBorder.SetActive(isSel);
                if (entries[i].stateBadge != null)
                    entries[i].stateBadge.color = isSel ? new Color(0.10f, 0.05f, 0.02f, 0.96f) : new Color(0.16f, 0.08f, 0.03f, 0.9f);
            }
            var e = entries[index];
            if (detailIcon != null)
            {
                detailIcon.sprite = e.item.icon;
                detailIcon.gameObject.SetActive(e.item.icon != null);
            }
            detailTitle.text = e.item.displayName;
            string extra = "";
            if (e.item.kind == ShopKind.Weapon && e.weapon != null)
                extra = "\nDaño " + e.weapon.damage + " · Velocidad x" + e.weapon.attackSpeed.ToString("0.0") + " · Alcance x" + e.weapon.reach.ToString("0.0");
            else
            {
                int lvl = PlayerProgress.GetUpgradeLevel(e.item.kind);
                int max = PlayerProgress.MaxUpgradeLevel(e.item);
                extra = "\nProgreso: Nivel " + lvl + " de " + max + (lvl < max ? "  (Siguiente nivel: " + e.item.prices[lvl] + " monedas)" : "  (Completada)");
            }
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
