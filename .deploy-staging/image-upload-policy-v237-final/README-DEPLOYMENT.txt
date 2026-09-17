V237 Image Upload Policy

JPG/JPEG/PNG images up to 5MB are accepted. The API validates the real format and dimensions, decodes and re-encodes every image, removes metadata, and stores it under a random key. PDF and WEBP uploads are rejected on every multipart endpoint. Chat and provider profile JSON upload paths use the same policy.

Database, API App_Data, and production appsettings files are not included or changed.
Run the outer deployment script from Windows PowerShell as Administrator after checking its ZIP hash.
