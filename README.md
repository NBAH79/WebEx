Пилот веб движка на websockets повторяющий логику SPA Blazor

Для коммуникации используется язык LSON, описание внутри
Создается сессия.
Сервисы обрабатывают интересные им запросы из очереди.
Страница состоит из контейнера и элементов с тремя функциями: Create, Render, Event
Элементы базируются на абстрактном классе WebComponent и Инстанциируются в контейнер.
Потоковая передача данных поддерживается в базе.

Тестовая страница открывается по адресу http://localhost:8080/index.html
