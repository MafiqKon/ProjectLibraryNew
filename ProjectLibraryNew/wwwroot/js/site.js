// ========================================================
// ГЛОБАЛНИ СКРИПТОВЕ ЗА PROJECT LIBRARY
// ========================================================

/**
 * 1. Показване/скриване на паролата (Адаптирано за Lucide иконки)
 */
function togglePasswordVisibility(button) {
    // Намираме контейнера и полето за парола
    var container = button.parentElement;
    var input = container.querySelector("input");
    var icon = button.querySelector("i");

    // Сменяме типа на полето и атрибута на иконката
    if (input.type === "password") {
        input.type = "text";
        icon.setAttribute("data-lucide", "eye-off"); // Задраскано око
    } else {
        input.type = "password";
        icon.setAttribute("data-lucide", "eye"); // Нормално око
    }

    // Задължително казваме на Lucide да преначертае иконката с новия атрибут
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * 2. Показване/скриване на отговорите към коментар (За мобилни устройства)
 */
function toggleMobileReplies(containerId, btn) {
    const container = document.getElementById(containerId);

    // Проверяваме дали в момента са показани
    if (container.classList.contains('show-replies-mobile') || container.classList.contains('d-block')) {
        // Скриване
        container.classList.remove('show-replies-mobile', 'd-block');
        container.classList.add('d-none');

        const count = btn.getAttribute('data-count');
        btn.innerHTML = `<i data-lucide="message-square-plus" class="me-2" style="width: 16px;"></i> Виж ${count} отговор${count == 1 ? 'а' : 'и'}`;
    } else {
        // Показване
        container.classList.remove('d-none');
        container.classList.add('show-replies-mobile');

        btn.innerHTML = `<i data-lucide="chevron-up" class="me-2" style="width: 16px;"></i> Скрий отговорите`;
    }

    // Преначертаваме новата иконка в бутона
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * 3. Инициализация при зареждане на страницата
 */
document.addEventListener("DOMContentLoaded", function () {
    // Гарантираме, че всички Lucide иконки по сайта се зареждат правилно
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
});

/**
* 4. AJAX Коментари (Без рефреш на страницата)
*/
$(document).on('submit', '.ajax-comment-form', function (e) {
    e.preventDefault(); // СПИРАМЕ РЕФРЕШВАНЕТО!

    var form = $(this);
    var btn = form.find('button[type="submit"]');
    var originalBtnHtml = btn.html();

    // Правим бутона да показва, че зарежда
    btn.html('<i data-lucide="loader-2" class="me-1" style="width: 14px; height: 14px; animation: spin 1s linear infinite;"></i>...');
    btn.prop('disabled', true);
    if (typeof lucide !== 'undefined') { lucide.createIcons(); }

    $.ajax({
        url: form.attr('action'),
        type: form.attr('method'),
        data: form.serialize(),
        success: function (response) {
            var parentId = form.find('input[name="parentCommentId"]').val();

            if (parentId) {
                // Ако е отговор (Reply), го закачаме в контейнера за отговори
                var repliesContainer = $('#replies-container-' + parentId);
                repliesContainer.append(response).removeClass('d-none').addClass('d-lg-block show-replies-mobile');

                // Скриваме формата за писане
                $('#reply-form-' + parentId).addClass('d-none');
            } else {
                // Ако е главен коментар, го закачаме най-отгоре в списъка
                $('#main-comments-section').prepend(response);
            }

            // Изчистваме полето за писане
            form[0].reset();

            // Преначертаваме иконките за новия коментар
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        error: function () {
            alert("Възникна грешка при изпращането на коментара.");
        },
        complete: function () {
            // Връщаме стария вид на бутона
            btn.html(originalBtnHtml);
            btn.prop('disabled', false);
            if (typeof lucide !== 'undefined') { lucide.createIcons(); }
        }
    });
});

/**
* 5. Умна Навигация (Smart Navbar)
* Крие се при скрол надолу, показва се при скрол нагоре
*/
document.addEventListener("DOMContentLoaded", function () {
    const el_autohide = document.querySelector('.smart-scroll');

    if (el_autohide) {
        let last_scroll_top = 0;
        window.addEventListener('scroll', function () {
            let scroll_top = window.scrollY;

            if (scroll_top < last_scroll_top) {
                // Скролираме НАГОРЕ -> Показваме менюто
                el_autohide.classList.remove('scrolled-down');
                el_autohide.classList.add('scrolled-up');
            }
            else if (scroll_top > 80) {
                // Скролираме НАДОЛУ (след първите 80px) -> Скриваме менюто
                el_autohide.classList.remove('scrolled-up');
                el_autohide.classList.add('scrolled-down');
            }

            last_scroll_top = scroll_top;
        });
    }
});