// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Dark mode toggle
const toggleBtn = document.getElementById('darkModeToggle');
const body = document.body;
if (localStorage.getItem('darkMode') === 'true') body.classList.add('dark-mode');
if (toggleBtn) {
    toggleBtn.addEventListener('click', () => {
        body.classList.toggle('dark-mode');
        localStorage.setItem('darkMode', body.classList.contains('dark-mode'));
    });
}
