document.addEventListener("DOMContentLoaded", (event) => {
    document.querySelectorAll('input[type="radio"]').forEach((radio) => {
        radio.addEventListener('change', () => {
            document.querySelectorAll(`input[name="${radio.name}"]`).forEach((input) => {
                input.parentElement.classList.remove('checked');
            });

            if (radio.checked) {
                radio.parentElement.classList.add('checked');
            }
        });
    });
});
document.addEventListener("DOMContentLoaded", (event) => document.getElementById('results').scrollIntoView());
