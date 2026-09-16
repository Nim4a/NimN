# NimN

Personal fork of [PattN](https://github.com/patterniha/PattN), based on [v2rayN](https://github.com/2dust/v2rayN). Original copyright and GPL-3.0 license are retained.

## تغییرات نسخه شخصی

- نام NimN و آیکون NI برای برنامه و وضعیت‌های سینی سیستم.
- تصویر پرچم کنار نام کانفیگ در رابط‌های WPF و Avalonia؛ بدون وابستگی به نمایش ایموجی ویندوز.
- کشور ابتدا از نتیجهٔ تست IP خروجی استخراج می‌شود. برای کانفیگ‌های بدون نتیجهٔ تست، IP عمومی آدرس سرور به‌صورت خودکار و غیرهم‌زمان با HTTPS از ipwho.is مکان‌یابی می‌شود؛ این کشور ممکن است با خروجی واقعی اتصال (مثلاً پشت CDN) متفاوت باشد. در نبود نتیجه، برچسب نام فقط یک راهنمای تقریبی است؛ کشور نامشخص حدس زده نمی‌شود.
- حریم خصوصی: برای مکان‌یابی فقط IP عمومی به ipwho.is فرستاده می‌شود، نه نام، رمز یا متن کانفیگ. آدرس‌های محلی مکان‌یابی نمی‌شوند. نتیجه‌ها با محدودیت نرخ و کش موقت نگهداری می‌شوند؛ تغییر گروه یا فیلتر، بررسی فهرست قبلی را لغو می‌کند.

## دانلود و تست

[Releases](https://github.com/Nim4a/NimN/releases) · [Build status](https://github.com/Nim4a/NimN/actions/workflows/nimn-release.yml)

Workflow اختصاصی، تست‌ها و دو رابط ویندوز x64 را می‌سازد. تگ‌های `v*-nimn.*` پس از موفقیت بیلد، نسخهٔ آزمایشی منتشر می‌کنند. فایل‌ها امضای دیجیتال ندارند؛ SHA-256 کنار ZIP قرار می‌گیرد. بیلد موفق جایگزین تست اتصال روی دستگاه نیست.

## Upstream

PattN adds Iran-focused defaults and support for cipherSuites / unsafe fingerprints, with a modified Xray core. Original upstream documentation and donation details: [PattN README](https://github.com/patterniha/PattN#readme).
