using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsIoT
{
    class Agenda
    {
        private string agendaDate;
        private string title;
        private string message;

        public string AgendaDate
        {
            get
            {
                return agendaDate;
            }
            set
            {
                agendaDate = value;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
            }
        }

        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

    }
}
