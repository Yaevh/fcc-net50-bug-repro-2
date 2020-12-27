# Szlem.Engine

Projekt zawiera główny silnik SZLEM, w tym:

* interfejs `ISzlemEngine` - główny silnik SZLEM

* wyjątki rzucane przez silnik

* przypadki użycia, w tym zapytania, komendy i zwrotki

* nazwy ról i polityk autoryzacji (konfigurowane przez projekty z implementacjami)

* interceptowy zapytań MediatR (`IPipelineBehavior<TRequest, TResponse>`)

* interfejsy potrzebne do działania silnika

Projekt napisany jest w architekturze **CQRS** (*Command/Query Responsibility Segregation*) aka [architektura selerowa](https://www.youtube.com/watch?v=SUiWfhAhgQw). Każdemu przypadkowi użycia powinna odpowiadać pojedyncza klasa (np. `UserManagement.RevokeRoleUseCase`, `Schools.DetailsUseCase`), która powinna definiować żądanie i odpowiedź.
Takie rozwiązanie pozwala używać tego samego silnika niezależnie od wybranej warstwy dostępu do danych i kontenera *IoC*. Z kolei warstwa prezentacji odpowiada jedynie za zbudowanie zapytania na podstawie zebranych danych (np. z zapytania JSON, formularza webowego lub inputu konsoli), wysłania tego zapytania do `ISzlemEngine` i wyrenderowanie odpowiedzi we właściwej formie (jako JSON, HTML, plik itp.).

## Struktura przypadku użycia
Przypadek użycia jest zdefiniowany w statycznej klasie o nazwie odpowiadającej nazwie tego przypadku (np. `UserManagement.RevokeRoleUseCase`, `Schools.DetailsUseCase`). Podklasami klasy z przypadkiem użycia są:

* definicja zapytania/komendy (np. `RevokeRoleUseCase.Command`, `DetailsUseCase.Query`) - powinna być oznaczona atrybutem `[Authorize(AuthorizationPolicies.MyPolicy]` określającym politykę autoryzacji (wymagania jakie musi spełnić użytkownik żeby wykonać zapytanie/komendę)

* definicja zwracanych danych (np. `DetailsUseCase.UserDetails`)

* *opcjonalnie* walidator poprawności komendy/zapytania (koniecznie publiczny)

* *opcjonalnie* dodatkowe zapytania i odpowiedzi związane z przypadkiem użycia

Każdy przypadek użycia musi posiadać handler wykonujący zapytanie/komendę. Powinien on być zdefiniowany w odpowiednim projekcie infrastruktury (np. `Szlem.Persistence.EF`), pod ścieżką odpowiadającą ścieżce przypadku użycia, oraz dodany do kontenera *IoC*.

## Inne katalogi
### Behaviors
Interceptory MediatR `IPipelineBehavior<TRequest, TResponse>`) przechwytujące żądanie i dokonujące na nim operacji (logowanie, walidacja, autoryzacja itp.). Zdefiniowane tu walidatory powinny zostać dodane do kontenera *IoC*.
### Exceptions
Wyjątki aplikacji spowodowane logiką biznesową (błędami walidacji i autoryzacji, próbą dostępu do nieistniejącego zasoby itp.). Powinny dziedziczyć po klasie `SzlemException`.
### Infrastructure
Serwisy pomocnicze.
### Interfaces
Interfejsy potrzebne do działania silnika. Kod wywołujący musi dostarczyć ich implementacje, wstrzykując je do kontenera *IoC*.
### AuthorizationPolicies
Deklaracje polityk autoryzacji (zestawów zasad, które musi spełnić użytkownik, aby wykonać żądanie). Definicje polityk docelowo zostaną przeniesione do `Szlem.Engine`, ale na chwilę obecną muszą być dostarczone przez kod wywołujący.
