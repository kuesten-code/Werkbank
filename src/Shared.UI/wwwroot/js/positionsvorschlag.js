// Blazor kann preventDefault nur pauschal pro Ereignis setzen. Hier wird gezielt nur die Taste
// unterdrückt, die gerade die Vorschlagsliste bedient: Enter bleibt sonst ein Zeilenumbruch.
export function verbinden(element) {
    element.addEventListener('keydown', event => {
        const offen = element.dataset.offen === 'true';
        const markiert = element.dataset.markiert === 'true';

        if ((offen && (event.key === 'ArrowDown' || event.key === 'ArrowUp' || event.key === 'Escape'))
            || (markiert && (event.key === 'Enter' || event.key === 'NumpadEnter'))) {
            event.preventDefault();
        }
    });
}
