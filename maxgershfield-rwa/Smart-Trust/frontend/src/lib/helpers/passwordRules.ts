export const passwordRules = [
  {
    text: "6 characters",
    check: (pwd: string) => pwd.length >= 6,
  },
  {
    text: "1 upper case",
    check: (pwd: string) => /[A-Z]/.test(pwd),
  },
  {
    text: "1 number",
    check: (pwd: string) => /\d/.test(pwd),
  },
];
