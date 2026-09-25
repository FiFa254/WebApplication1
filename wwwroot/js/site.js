document.addEventListener("DOMContentLoaded", function () {
    var toggle = document.getElementById("navToggle");
    var nav = document.getElementById("siteNav");

    if (!toggle || !nav) {
        return;
    }

    toggle.addEventListener("click", function () {
        var isOpen = nav.classList.toggle("is-open");
        toggle.setAttribute("aria-expanded", isOpen ? "true" : "false");
    });

    nav.querySelectorAll(".site-nav-link").forEach(function (link) {
        link.addEventListener("click", function () {
            nav.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
        });
    });
});

// Ask before submitting forms marked with data-confirm (e.g. delete profile).
document.addEventListener("submit", function (event) {
    var message = event.target.getAttribute && event.target.getAttribute("data-confirm");
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});
