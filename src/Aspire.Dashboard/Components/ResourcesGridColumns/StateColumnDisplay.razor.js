export function addWaitForMouseEventListeners() {
    document
        .querySelectorAll('.state-column-cell')
        .forEach(cell => {
            if (!cell.areMouseEventsHooked) {
                cell.addEventListener('mouseenter', function (event) {
                    //if (event.target && event.target.classList.contains('state-column-cell')) {
                        highlightRelatedElements(event.target);
                    //}
                });

                cell.addEventListener('mouseleave', function (event) {
                    //if (event.target && event.target.classList.contains('state-column-cell')) {
                        removeHighlightRelatedElements(event.target);
                    //}
                });

                cell.areMouseEventsHooked = true;
            }
        });
}

function highlightRelatedElements(element) {
    const waitsFor = element.dataset.waitsFor;
    const relatedRows = waitsFor ? waitsFor.split(' ') : [];

    relatedRows.forEach(relatedRow => {
        document
            .querySelectorAll(`.state-column-cell[data-resource-name="${relatedRow}"]`)
            .forEach(cell => {
                console.log('highlighting cell with resource name:', relatedRow);
                cell.parentElement.classList.add('highlight');
            });
    });
}

function removeHighlightRelatedElements(element) {
    const waitsFor = element.dataset.waitsFor;
    const relatedRows = waitsFor ? waitsFor.split(' ') : [];

    relatedRows.forEach(relatedRow => {
        document
            .querySelectorAll(`.state-column-cell[data-resource-name="${relatedRow}"]`)
            .forEach(cell => cell.parentElement.classList.remove('highlight'));
    });
}
