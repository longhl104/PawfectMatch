export interface ContactInfo {
  email: string;
  phone: string;
  facebook: string;
  instagram: string;
}

export const CONTACT_INFO: ContactInfo = {
  email: 'yenngo20199@gmail.com',
  phone: '+61451920109',
  facebook: 'https://www.facebook.com/ngo.hai.yen.932779',
  instagram: 'https://www.instagram.com/n.g.o_haiyen/'
};

export const CONTACT_ACTIONS = {
  openEmail: (email: string) => {
    window.open(`mailto:${email}`, '_blank');
  },

  callPhone: (phone: string) => {
    window.open(`tel:${phone}`, '_blank');
  },

  openUrl: (url: string) => {
    window.open(url, '_blank', 'noopener,noreferrer');
  }
};
