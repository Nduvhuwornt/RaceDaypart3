// Confirm before destructive actions: <form data-confirm="Are you sure?">
document.querySelectorAll('form[data-confirm]').forEach(function (form) {
    form.addEventListener('submit', function (event) {
        if (!window.confirm(form.dataset.confirm)) {
            event.preventDefault();
        }
    });
});

// Live preview for image inputs: <input type="file" data-preview="#previewImage">
document.querySelectorAll('input[type="file"][data-preview]').forEach(function (input) {
    input.addEventListener('change', function () {
        var target = document.querySelector(input.dataset.preview);
        if (!target || !input.files || !input.files[0]) { return; }
        target.src = URL.createObjectURL(input.files[0]);
        target.classList.remove('d-none');
    });
});

// Registration: show the organiser code field only when "Organiser" is chosen
(function () {
    var radios = document.querySelectorAll('input[name="Role"]');
    var codeBox = document.getElementById('organiserCodeBox');
    if (!radios.length || !codeBox) { return; }

    function update() {
        var selected = document.querySelector('input[name="Role"]:checked');
        codeBox.classList.toggle('d-none', !selected || selected.value !== 'Organiser');
    }

    radios.forEach(function (radio) { radio.addEventListener('change', update); });
    update();
})();
