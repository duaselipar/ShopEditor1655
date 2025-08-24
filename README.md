# EO Shop/NewShop Editor

A Windows Forms tool for editing **Eudemons Online** client shop configuration files and syncing them with a MySQL database.

## ✨ Features
- **All Shop (`shop.dat`)**
  - View/edit shop list & items
  - Add shops, rename, manage item lists

- **VIP Shop/Mall (`newshopmx.dat` / `newshopmx.ini`)**
  - Category tree with item list
  - `[0]` auto-shown as **Home** if no `Name=`
  - Add/remove mall items
  - Drag & drop order, right-click **Move to top/bottom**, **Delete selected**

- **Wardrobe Buy (`dressroomitem.ini`)**
  - Tabs: Casual, Weapon Soul, Avatar, Decoration, Toy, Hair, Follow Pet, Eudemon Skin
  - **Price sync**: edit price in Shop/NewShop/Wardrobe → semua tab lain ikut
  - **Casual/Weapon/Decoration/Toy**: show item **Name** from `itemtype.fdb` (read-only)
  - **Avatar/Hair**:
    - Gender mapping: `1=Male`, `2=Female`, `64=Male(SS)`, `128=Female(SS)`
    - Save to DB:
      - `cq_faceinfotype` (Avatar) and `cq_hairinfotype` (Hair)
      - `buyable` tick ➜ `pricetype1/2` on, `price1/2` ikut field, `changeprice1/2=10000`
      - Untick ➜ clear related price fields to **0**
    - Per-row male/female price supported (not synced)
  - **Follow Pet**:
    - Convert **FollowPetInfo.dat ⇄ .ini** (GBK, XOR) + auto sort
    - Edit fields (`Type/Name/MoveSpeed/.../Collection`)
    - DB sync: `cq_packpetinfotype` (`id=Type`, `money=NeedMoney`)
  - **Eudemon Skin**
    - Load `EudLookInfo.ini` (id read-only)
    - Edit/save to DB `cq_eudlookinfotype` (LookType, NeedEmoney, NeedStarLev, UseEudType1..3, GetBackLook*…)

- **Servant Craft (`composeconfig.ini`)**
  - Split: **Gift Master** (needtype=1) & **Spirit Master** (needtype=2)
  - Grids sorted by `needlevel ASC, proficiency ASC`
  - Add item via **NewServantItem**:
    - Pick main item + up to 5 required items (ID & Name from `itemtype.fdb`)
    - Numeric guards (level 1–6, max item 1–5, counts numeric)
  - DB upsert: `cq_goddesscomposeconfig` with unique `(needtype,item)`
  - Right-click **Delete selected**, fixed read-only cols (needtype, item IDs, names)

- **Event Shop (`activitynewshop.ini`)**
  - Tabs: **Astra (1)**, **Honor (2)**, **Plane (4)**
  - Columns: `shop_type` & `id` hidden, `Itemtype` read-only + `ItemName` from `itemtype.fdb`
  - Add via **NewEventItem** dialog:
    - Fast search (ID/Name contains): **Enter** = next match, **Ctrl+Enter**/**Add** = confirm
    - `Talent_coin` from **txtPrice** (must be **≥1**), default `Version=49213`
  - Reorder: drag & drop, right-click **Move to top/bottom**, **Delete selected**
  - Save: reindex `id` **sequentially across all shops** (Astra→Honor→Plane)

- **Extra**
  - Reads **itemtype.fdb** (names & prices)
  - Auto-refresh **cq_goods** & **cq_collectiongoods** (preserves `[25]` limits via snapshot)
  - Connection guard: tabs disabled until MySQL connected
  - Fixed column widths on key grids; no user resize; smooth horizontal scroll
  - Double-buffered grids for fast UI

## ⚙️ Requirements
- .NET 6.0+ (WinForms)
- MySQL 8.0+
- EO client files in `ini/`:
  - `shop.dat`, `newshop.dat`, `newshopmx.dat`
  - `dressroomitem.ini`, `composeconfig.ini`, `EudLookInfo.ini`
  - `FollowPetInfo.dat`/`.ini` (converter built-in)
  - `itemtype.fdb`

## 📖 Usage
1. Open `ShopEditor.sln`, build & run.
2. Fill MySQL (host/port/user/pass/db) and select client folder (must contain `ini/`).
3. **Connect** → tabs enabled & files loaded.
4. Edit in tabs. Use **NewServantItem/NewEventItem** to add entries with item picker.
5. **Save** to write `.ini/.dat` & sync DB tables. Event Shop IDs auto-reindexed.

## 📝 Notes
- Hidden shop IDs (see `ShopDatHandler.hiddenShopIDs`) are ignored when pushing to DB.
- Wardrobe: `No/ItemID/Name/Gender` columns are fixed width & non-resizable.
- Event Shop: `Itemtype` is locked; `shop_type` & `id` are hidden; ID sequence rebuilt on save.
- Always backup your client files before editing.
