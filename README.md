# AutoPartsShop.API 🚗🛠️  
ASP.NET Core Web API – Autóalkatrész Webshop Backend

## 🧾 Áttekintés

Az **AutoPartsShop.API** projekt egy teljes funkcionalitású autóalkatrész webshop kiszolgáló oldali (backend) alkalmazása.  
REST-alapú API-kat biztosít a felhasználói regisztrációhoz és bejelentkezéshez, termékek (alkatrészek, felszerelések) listázásához, kosárkezeléshez, rendelés leadásához és adminisztratív funkciókhoz.

A rendszer **ASP.NET Core Web API** alapokra épül, az adatkezeléshez **Entity Framework Core** technológiát, a biztonság biztosításához pedig **JWT autentikációt** használ.

---

## 🛠️ Technológiák

- ASP.NET Core 7.0
- Entity Framework Core
- SQL Server
- JWT (JSON Web Token) autentikáció
- AutoMapper
- FluentValidation
- CORS támogatás a frontend számára
- Szerepkör alapú jogosultságkezelés (felhasználó / admin)
- Dependency Injection

---

## 🧩 Fő funkciók

### Felhasználói oldal:
- Regisztráció, bejelentkezés (JWT tokennel)
- Profil megtekintése, szerkesztése rendeléstörténet
- Kosárkezelés, rendelés leadása
- Fizetési mód kiválasztása (készpénz, bankkártya, online)
- E-mail értesítés rendelés állapotváltozásról

### Admin oldal:
- Admin bejelentkezés és védett API hozzáférés
- Gépjármű márkák és modellek kezelése
- Alkatrészek, felszerelések és kategóriáik kezelése
- Rendelések státuszának módosítása és ügyfél értesítés
- Termékképek feltöltése
- Enum alapú státuszok, fizetési és szállítási módok

---

## 🔐 Hitelesítés

- JWT token alapú hitelesítés
- Szerepkörök:
  - `User` – átlagos felhasználó
  - `Admin` – rendszergazda
- Token továbbítása: `Authorization: Bearer <token>`

---

## 🗃️ Adatbázis

- EF Core Code First használatban
- Táblák automatikusan létrejönnek első futáskor

---

## ▶️ Telepítés

### Előfeltételek:
- .NET 7 SDK
- SQL Server 
- Visual Studio vagy VS Code

### `appsettings.json` konfigurálása:
Hozz létre egy `appsettings.json` fájlt a projekt gyökerében az alábbi tartalommal:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AutoPartsShopDb;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "SecretKey": "titkos-jwt-kulcs",
    "Issuer": "AutoPartsShop",
    "Audience": "AutoPartsUsers"
  },
  "EmailSettings": {
    "From": "noreply@autopartsshop.hu",
    "SmtpServer": "smtp.example.com",
    "Port": 587,
    "Username": "smtp-felhasznalo",
    "Password": "smtp-jelszo"
  }
}
```

> 🔐 A valós adatokat igénylő helyekre írj be saját értékeket.

### Indítás:
```
dotnet run
```

---

## 📬 API végpontok

| Módszer | URL | Leírás |
|--------|-----|--------|
| `POST` | `/api/users/register` | Felhasználó regisztráció |
| `POST` | `/api/users/login` | Bejelentkezés |
| `GET` | `/api/parts` | Alkatrészek listázása |
| `GET` | `/api/equipments` | Felszerelések listázása |
| `POST` | `/api/orders/create` | Rendelés leadása |
| `PUT` | `/api/orders/update-status/{id}` | Admin: rendelés státusz frissítése |

További végpontok elérhetők az admin funkciókhoz (CRUD: gépjármű, alkatrész, kategória stb.).

---

## 📁 Projekt struktúra

```
AutoPartsShop.API/
│
├── Controllers/          # API vezérlők
├── Program.cs            # Belépési pont
└── appsettings.json      # Konfiguráció

AutoPartsShop.Core/
│
├── Enums/          # API vezérlők
├── DTOs/                 # Adatátviteli objektumok
├── Models/               # Adatmodellek
├── Helpers/              # JWT, e-mail, enumok stb.

AutoPartsShop.Infrastructure/
│
├── Migrations/          # API vezérlők
├── Services/                 # Adatátviteli objektumok
└── appDbContext.cs      # Adatbázis Konfiguráció

AutoPartsShop.Tests/
└── OrdesControllerTests.cs      # Adatbázis Konfiguráció
└── UserControllerTests.cs      # Adatbázis Konfiguráció
```

---

## 📧 E-mail értesítések

A rendelés státuszának frissítésekor a rendszer automatikusan értesítést küld az ügyfélnek SMTP-n keresztül.  
A küldéshez szükséges adatokat az `appsettings.json` fájlban kell megadni.

---

## 🖼️ Termékkép feltöltés

Az admin felület lehetővé teszi termékképek feltöltését.  
A képek a szerver fájlrendszerébe kerülnek, az elérési útvonalukat pedig az adatbázis tárolja.

---

## 📄 Licenc

Ez a projekt tanulási és demonstrációs célra készült.  
Szabadon használható, bővíthető és testreszabható egyéni célra.
