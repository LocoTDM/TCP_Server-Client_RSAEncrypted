private void btnImportKey_Click(object sender, EventArgs e)
        {
            OpenFileDialog opf = new OpenFileDialog();
            if(opf.ShowDialog() == DialogResult.OK)
            {
                string path = opf.FileName;
                String strPrivateParameters = "";
                StreamReader sr = new StreamReader(path);
                // lexojme filen e hapur deri ne fund
                strPrivateParameters = sr.ReadToEnd();
                sr.Close();
                try
                {
                    // tentojme te ia japim filen e lexuar objektit RSA (nese nuk eshte file i mire,
                    // kapet gabimi nga Catch, pra nese nuk eshte file xml qe permban celesa RSA)
                    objRSAServer.FromXmlString(strPrivateParameters);
                    // meqe ka mundesi qe te importohet vetem celesei publik, atehere pyesim a permban
                    // ai xml file tagun <D> (celesin privat), meqe ky gabim nuk kapet nga kodi i siperm
                    if (!strPrivateParameters.Contains("<D>"))
                    {
                        throw new Exception("Imported Public Key");
                    }
                    keysImported = true;
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Please import valid RSA keys!","Error");
                }
            }
        }
    }


}