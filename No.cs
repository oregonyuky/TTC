using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessamentoImagens
{
    internal unsafe class No
    {
        private Info info;
        private No prox;

        public No(Info info, No prox)
        {
            this.info = info;
            this.prox = prox;
        }

        public No(Info info)
        {
            this.info = info;
            this.prox = null;
        }

        public No()
        {
            this.info = null;
            this.prox = null;
        }

        public Info getInfo()
        {
            return this.info;
        }

        public void setInfo(Info info)
        {
            this.info = info;
        }

        public No getProx()
        {
            return prox;
        }

        public void setProx(No prox)
        {
            this.prox = prox;
        }
    }
}
