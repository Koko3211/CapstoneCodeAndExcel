import pandas as pd
import matplotlib.pyplot as plt

# This script visualizes the top 10 integrations with the most reported issues
# by processing data collected by querying the GitHub API for issues over a 6-month period.

data = pd.read_csv('C:/Users/cassi/OneDrive/Dokumentumok/Uni stuff/Capstone/igibs/igiBsproject/issueCount1.txt')
topDefects = data.value_counts().reset_index(drop=False)
topDefects = topDefects[topDefects['Integrations'] != '_No response_' ]
topDefects = topDefects[topDefects['Integrations'] != 'no integration specified/Wrong format']
topDefects = topDefects[topDefects['Integrations'] != 'alexa_devices']

#topDefects.drop(labels=["_No response_", "no integration specified/Wrong format"], axis=0, inplace=True)
print(topDefects.head(10))
print(data.shape)
plt.barh( topDefects[topDefects.columns[0]].head(10), topDefects[topDefects.columns[1]].head(10), align='center')
plt.gca().invert_yaxis()
plt.xlabel('Number of issues')
plt.ylabel('Integrations')
plt.title('Top 10 integrations with the most issues reported in the 6 months')
plt.tight_layout()
plt.show()
