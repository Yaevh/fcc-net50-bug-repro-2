# SZLEM
**SZLEM** to **S**ystem **Z**arządzania **L**ekcjami **E**konomii dla **M**łodzieży.

## Konfiguracja
Pliki konfiguracyjne powinny znaleźć się w katalogu `ApplicationData/Szlem`. Na systemie Windows jest to `C:\Users\username\AppData\Roaming\Szlem`, na systemie Linux `/home/username/.config/Szlem`.

Jeśli konfiguracja nie zostanie znaleziona w katalogu `ApplicationData/Szlem`, używana jest konfiguracja domyślna zamieszczona w katalogu `./config`.

### E-mail
Do wysyłania e-maili wykorzystywana jest biblioteka MailKit. Jej konfiguracja znajduje się w sekcji `EmailOptions` pliku `appsettings.json`. Niektóre serwery poczty (np. GMail) domyślnie używają uwierzytelniania dwuetapowego (_two-factor authentication_). Obecnie **SZLEM** nie wspiera 2FA, więc aby używać konta GMail do wysyłki maili należy wyłączyć 2FA w ustawieniach konta (https://myaccount.google.com/u/0/lesssecureapps?pli=1&pageId=none). **UWAGA**: wyłączenie 2FA może ułatwić włamanie na konto, pomyśl dwa razy zanim to zrobisz.

## Architektura
Projekt powinien być pisany zgodnie z regułami Domain-Driven Design i rozbity na moduły zgodnie z granicami bounded contextów. Niektóre agregaty wykorzystują bazę danych SQL, a inne korzystają z metody Event Sourcing.

### Nazewnictwo agregatów i eventów
Eventy opisujące agregaty działające wg metody Event Sourcing muszą mieć globalnie unikalne nazwy. Każdy event musi obowiązkowo mieć atrybut `[EventVersion]` z unikalną nazwą złożoną z przedrostka `Szlem`, nazwy modułu, nazwy agregatu (moduł zawiera tylko jeden agregat to jego nazwę można pominąć) i nazwy własnej eventu (np. `[EventVersion("Szlem.Recruitment.Enrollment.ContactOccured", 1)]`).

Agregaty mogą mieć nadane unikalne nazwy przy pomocy atrybutu `[AggregateName]`, choć nie jest to konieczne do poprawnego działania systemu, a jedynie przydatne dla większej czytelności. Nazwy agregatów powinny być tworzone według analogicznych zasad jak nazwy eventów.