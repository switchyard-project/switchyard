// Coordinates stay one-based in the API. One cell represents one square metre.
export function rectangleBetween(start, end) {
    return {
        startRow: Math.min(start.row, end.row),
        startColumn: Math.min(start.column, end.column),
        rows: Math.abs(start.row - end.row) + 1,
        columns: Math.abs(start.column - end.column) + 1
    };
}

export function cellAtPoint(x, y, bounds, warehouse) {
    return {
        row: Math.max(1, Math.min(warehouse.rows, Math.floor((y - bounds.top) / bounds.height * warehouse.rows) + 1)),
        column: Math.max(1, Math.min(warehouse.columns, Math.floor((x - bounds.left) / bounds.width * warehouse.columns) + 1))
    };
}

export function placementError(rectangle, warehouse, zones) {
    const { startRow, startColumn, rows, columns } = rectangle;
    if (![startRow, startColumn, rows, columns].every(value => Number.isInteger(value) && value > 0)) {
        return "Indica medidas enteras de al menos 1 metro y posiciones válidas.";
    }
    if (startRow + rows - 1 > warehouse.rows || startColumn + columns - 1 > warehouse.columns) {
        return "La zona sale del almacén. Reduce sus medidas o cambia su posición.";
    }
    if (zones.some(zone => zone.startRow < startRow + rows && zone.startRow + zone.rows > startRow
        && zone.startColumn < startColumn + columns && zone.startColumn + zone.columns > startColumn)) {
        return "La zona se superpone a otra. Dibuja en un espacio libre.";
    }
    return null;
}
