# ARCHGUARD AI
## Yapay Zeka Destekli BIM & Yönetmelik Uyumluluk Denetim Platformu
**Teknik Mimari ve Uçtan Uca Çalışma Raporu**  

---

### 1. Proje Özeti ve Çözülen Problem
Mimari projelerin (BIM / IFC / Revit) imar ve yangın yönetmeliklerine uygunluğunun manuel kontrolü zaman alıcı ve insan hatasına açıktır. **ArchGuard AI**, mimari modelleri doğrudan CAD ortamından veya IFC dosyalarından okuyarak, vektör veritabanında indekslenmiş resmi yönetmelik maddeleriyle (RAG) karşılaştıran ve çoklu LLM orkestrasyonu ile yapılandırılmış denetim raporu üreten uçtan uca bir otomasyon platformudur.

<img width="945" height="532" alt="image" src="https://github.com/user-attachments/assets/6af645b3-1772-4be8-837d-cbe6874f49c4" />

---

### 2. Sistem Mimarisi ve Teknoloji Yığını
* **Backend:** .NET 9.0, Python, ASP.NET Core Web API, Clean Architecture, CQRS, Entity Framework Core
* **Veritabanı & Vektör Arama:** PostgreSQL 16 (BIM Elemanları ve Denetim Bulguları), Qdrant Vector DB (Yönetmelik PDF Embedding)
* **Yapay Zeka & Ajan Protokolleri:** Microsoft Semantic Kernel, Google Gemini API, NVIDIA NIM, Model Context Protocol (MCP)
* **CAD Entegrasyonu:** Autodesk Revit 2022 C# Eklentisi (.NET Framework 4.8, Revit API)
* **Frontend:** React, TypeScript, Tailwind CSS

---

### 3. Adım Adım Çalışma Akışı ve Canlı Test Kanıtları

#### Adım 1: Mikroservis Altyapısı (Docker, PostgreSQL & Qdrant)
Sistemin ilişkisel ve vektör veritabanı katmanları Docker Compose üzerinde izole konteynerler olarak yapılandırılmıştır.

<img width="945" height="385" alt="image" src="https://github.com/user-attachments/assets/b9be8bf6-c2ab-4f0b-90d4-29b71c3ec09f" />

#### Adım 2: Yönetmelik PDF İndeksleme ve Qdrant Anlamsal Arama (RAG)
Resmi İmar ve Yangın Yönetmeliği PDF dokümanları parçalanarak (chunking) vektörleştirilir ve Qdrant koleksiyonuna kaydedilir. Denetim sırasında ilgili maddeler anlamsal benzerlik skoruyla otomatik getirilir. Burada kullanılmak üzere “BİNALARIN YANGINDAN KORUNMASI HAKKINDA YÖNETMELİK” pdf olarak sisteme yüklenmiştir.

<img width="945" height="532" alt="image" src="https://github.com/user-attachments/assets/63cd7777-3e1d-4c52-9b76-5f4d19da6ca3" />

#### Adım 3: IFC Model Ayrıştırma ve Otomatik Metraj Çıkarımı
Sisteme yüklenen `.ifc` dosyaları ayrıştırılarak kat, mahal, kapı genişlik/yükseklik ölçüleri ve duvar alan/hacim metrajları PostgreSQL veritabanına kaydedilir. Burada örnek olarak “AC20-FZK-Haus.ifc” dosyası kullanılmıştır.

<img width="945" height="531" alt="image" src="https://github.com/user-attachments/assets/b6459688-27d9-4ca0-9777-26a2880a53f5" />

#### Adım 4: Çoklu LLM Destekli Otomatik Yönetmelik Denetimi
Kullanıcı tek tıkla **Google Gemini** veya **NVIDIA NIM** sağlayıcılarını seçerek denetimi başlatır. Yapay zeka, model elemanlarını RAG'dan gelen yönetmelik maddeleriyle kıyaslayarak *Kritik İhlal (Error)*, *Uyarı (Warning)* ve *Uygun (Passed)* şeklinde yapılandırılmış JSON raporu üretir.

<img width="945" height="531" alt="image" src="https://github.com/user-attachments/assets/97c39f02-f95c-4574-98ae-774ebb057a5c" />

#### Adım 5: Veritabanı Bağlantılı BIM Asistanı (Semantic Kernel Tool Calling)
Yapay zeka asistanı, rastgele tahmin yürütmek yerine `BimModelTools` fonksiyonlarını çağırarak doğrudan PostgreSQL veritabanından gerçek metraj ve eleman sorguları gerçekleştirir.

<img width="945" height="531" alt="image" src="https://github.com/user-attachments/assets/9c618991-e76b-471a-8863-a3bb7654d817" />

#### Adım 6: Autodesk Revit 2022 Eklentisi ile Doğrudan Senkronizasyon
Geliştirilen özel C# Revit eklentisi sayesinde, model dışarıya IFC olarak aktarılmaya gerek kalmadan Revit içindeki *"ArchGuard AI'ya Gönder"* butonuyla doğrudan API'ye iletilir ve React arayüzüne otomatik (polling) olarak yansır.

<img width="945" height="512" alt="image" src="https://github.com/user-attachments/assets/f0fee465-d054-4803-be56-c46c9bb0891c" />

<img width="945" height="531" alt="image" src="https://github.com/user-attachments/assets/ebfa72ae-217f-4adc-8c38-213f79f0f360" />

#### Adım 7: Claude Desktop & Model Context Protocol (MCP) Entegrasyonu
ArchGuard AI, JSON-RPC 2.0 tabanlı yerel bir MCP sunucusu (`mcp-bridge.js`) sunar. Bu sayede Claude Desktop gibi harici yapay zeka ajanları doğrudan şirketin BIM veritabanına bağlanarak metraj ve uygunluk analizi yapabilir.

<img width="945" height="648" alt="image" src="https://github.com/user-attachments/assets/6b29040d-04a7-4dad-9d38-530fca184ee8" />

<img width="945" height="515" alt="image" src="https://github.com/user-attachments/assets/2f1773f0-017f-43ec-b730-fb07f418255d" />

