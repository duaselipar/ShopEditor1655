# EO Shop/NewShop Editor

A Windows Forms tool for editing **Eudemons Online** client shop configuration files and syncing them with a MySQL database.

## ✨ Features
- **All Shop (shop.dat)**  
  - View and edit shops/items  
  - Add new shops, update names, and manage item lists  

- **VIP Shop/Mall (newshopmx.dat / newshopmx.ini)**  
  - Category tree view with item list  
  - Auto-fallback: category `[0]` displays as **Home** if no `Name=` present  
  - Support for adding/removing mall items  

- **Wardrobe Buy (dressroomitem.ini)**  
  - Separate tabs for Casual, Weapon Soul, Avatar, Decoration, Toy, Hair, Follow Pet, Eudemon Skin  
  - Load & save wardrobe buy data  

- **Servant Craft (composeconfig.ini)**  
  - Gift Master and Spirit Master crafting data  
  - Add new servant craft items  

- **Extra Support**  
  - Reads **itemtype.fdb** for item names & price (Gold/Emoney)  
  - Auto-refresh **cq_goods** and **cq_collectiongoods** tables in database  
  - Connection guard → tabs disabled until MySQL is connected  
  - All message dialogs in English  

## ⚙️ Requirements
- .NET 6.0+ (WinForms)  
- MySQL 8.0+  
- Eudemons Online client files (`shop.dat`, `newshop.dat`, `newshopmx.dat`, `dressroomitem.ini`, `composeconfig.ini`, `itemtype.fdb`)  

## 📖 Usage
1. Build & run the project (`ShopEditor.sln`) in Visual Studio.  
2. Enter MySQL connection details (host, port, user, pass, database).  
3. Select client folder path (must contain `ini/` folder).  
4. Click **Connect** → tabs will be enabled and files loaded.  
5. Make changes in the editor.  
6. Click **Save** to write back `.ini/.dat` files and update database tables.  

## 🗂️ Project Structure
- `ShopEditor.cs` – main form logic  
- `ShopEditor.Designer.cs` – WinForms designer code  
- `ShopDatHandler.cs` – reader/writer for `shop.dat`  
- `NewShopDatHandler.cs` – handler for `newshop.dat`  
- `NewShopMxDatHandler.cs` – handler for `newshopmx.dat`  
- `FDBLoaderEPLStyle.cs` – itemtype.fdb reader  

## 📝 Notes
- Category `[0]` in `newshopmx.ini` with no `Name=` will be shown as **Home**.  
- Hidden shop IDs (defined in `ShopDatHandler.hiddenShopIDs`) remain untouched.  
- Always backup your client files before editing.  

