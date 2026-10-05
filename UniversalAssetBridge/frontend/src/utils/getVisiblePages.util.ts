export const getVisiblePages = (
  current: number,
  total: number
): (number | "...")[] => {
  const delta = 1;
  const range: (number | "...")[] = [];
  const left = current - delta;
  const right = current + delta;

  for (let i = 1; i <= total; i++) {
    if (i === 1 || i === total || (i >= left && i <= right)) {
      range.push(i);
    } else if (range[range.length - 1] !== "...") {
      range.push("...");
    }
  }
  return range;
};
