document.addEventListener("DOMContentLoaded", function () {
    var toggle = document.getElementById("toggleSidebar");
    var sidebar = document.getElementById("sidebar");

    if (!toggle || !sidebar) {
        return;
    }

    toggle.addEventListener("click", function () {
        sidebar.classList.toggle("collapsed");
    });
});
