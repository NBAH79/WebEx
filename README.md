Пилот веб движка на websockets повторяющий логику SPA Blazor

Для коммуникации используется язык LSON, описание внутри
Создается сессия.
Сервисы обрабатывают интересные им запросы из очереди.
Страница состоит из контейнера и элементов с тремя функциями: Create, Render, Event
Элементы базируются на абстрактном классе WebComponent и Инстанциируются в контейнер.
Потоковая передача данных поддерживается в базе.

LSON это протокол RPC, синтаксис описан в [protocol.txt](https://github.com/NBAH79/WebEx/blob/master/WebEx/Protocol.txt)

Тестовая страница открывается по адресу http://localhost:8080/index.html

![system drawio](https://github.com/NBAH79/WebEx/assets/11296963/3ef5d124-0b6d-40e1-8f71-9d6782ce33cc)

![process drawio](https://github.com/NBAH79/WebEx/assets/11296963/783daaa4-0337-46cd-82ea-6533cf718ebe)
